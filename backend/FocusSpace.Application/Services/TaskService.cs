using FocusSpace.Application.DTOs;
using FocusSpace.Application.Interfaces;
using Serilog;
using DomainTask = FocusSpace.Domain.Entities.Task;

namespace FocusSpace.Application.Services
{
    /// <summary>
    /// Implements business logic for task management use-cases.
    /// </summary>
    public class TaskService : ITaskService
    {
        private readonly ITaskRepository _taskRepository;
        private readonly ILogger _logger = Log.ForContext<TaskService>();

        public TaskService(ITaskRepository taskRepository)
        {
            _taskRepository = taskRepository;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<TaskDto>> GetTasksByUserIdAsync(int userId)
        {
            if (userId <= 0)
            {
                _logger.Warning("GetTasksByUserIdAsync called with invalid userId: {UserId}", userId);
                throw new ArgumentException("UserId must be a positive integer.", nameof(userId));
            }

            _logger.Information("Fetching tasks for user {UserId}", userId);

            var tasks = await _taskRepository.GetAllByUserIdAsync(userId);
            var result = tasks.Select(MapToDto).ToList();

            _logger.Information("Found {Count} tasks for user {UserId}", result.Count, userId);

            return result;
        }

        /// <inheritdoc />
        public async Task<TaskDto?> GetTaskByIdAsync(int id)
        {
            if (id <= 0)
            {
                _logger.Warning("GetTaskByIdAsync called with invalid id: {Id}", id);
                throw new ArgumentException("Task id must be a positive integer.", nameof(id));
            }

            _logger.Information("Fetching task {TaskId}", id);

            var task = await _taskRepository.GetByIdAsync(id);

            if (task is null)
            {
                _logger.Warning("Task {TaskId} not found", id);
                return null;
            }

            return MapToDto(task);
        }

        /// <inheritdoc />
        public async Task<TaskDto> CreateTaskAsync(CreateTaskDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new ArgumentException("Task title cannot be empty.", nameof(dto));

            if (dto.UserId <= 0)
                throw new ArgumentException("UserId must be a positive integer.", nameof(dto));

            _logger.Information("Creating task '{Title}' for user {UserId}", dto.Title, dto.UserId);

            var entity = new DomainTask
            {
                UserId      = dto.UserId,
                Title       = dto.Title.Trim(),
                Description = dto.Description?.Trim(),
                Priority    = dto.Priority,
                CreatedAt   = DateTime.UtcNow,
                UpdatedAt   = DateTime.UtcNow
            };

            var created = await _taskRepository.CreateAsync(entity);

            _logger.Information("Task {TaskId} created for user {UserId}", created.Id, dto.UserId);

            return MapToDto(created);
        }

        /// <inheritdoc />
        public async Task<TaskDto?> UpdateTaskAsync(UpdateTaskDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            if (dto.Id <= 0)
                throw new ArgumentException("Task id must be a positive integer.", nameof(dto));

            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new ArgumentException("Task title cannot be empty.", nameof(dto));

            _logger.Information("Updating task {TaskId}", dto.Id);

            var existing = await _taskRepository.GetByIdAsync(dto.Id);

            if (existing is null)
            {
                _logger.Warning("Update failed — task {TaskId} not found", dto.Id);
                return null;
            }

            existing.Title       = dto.Title.Trim();
            existing.Description = dto.Description?.Trim();
            existing.Priority    = dto.Priority;
            existing.UpdatedAt   = DateTime.UtcNow;

            var updated = await _taskRepository.UpdateAsync(existing);

            _logger.Information("Task {TaskId} updated successfully", dto.Id);

            return MapToDto(updated);
        }

        /// <inheritdoc />
        public async Task<bool> DeleteTaskAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Task id must be a positive integer.", nameof(id));

            _logger.Information("Deleting task {TaskId}", id);

            var exists = await _taskRepository.ExistsAsync(id);

            if (!exists)
            {
                _logger.Warning("Delete failed — task {TaskId} not found", id);
                return false;
            }

            await _taskRepository.DeleteAsync(id);

            _logger.Information("Task {TaskId} deleted successfully", id);

            return true;
        }

        // ──────────────────────────────────────────────────────────────
        // Private helpers
        // ──────────────────────────────────────────────────────────────

        private static TaskDto MapToDto(DomainTask task) => new()
        {
            Id          = task.Id,
            UserId      = task.UserId,
            Title       = task.Title,
            Description = task.Description,
            Priority    = task.Priority,
            CreatedAt   = task.CreatedAt,
            UpdatedAt   = task.UpdatedAt
        };
    }
}