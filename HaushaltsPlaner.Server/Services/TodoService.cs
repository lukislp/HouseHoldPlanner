using Microsoft.EntityFrameworkCore;
using HaushaltsPlaner.Server.Data;
using HaushaltsPlaner.Shared.DTOs;
using HaushaltsPlaner.Shared.Models;

namespace HaushaltsPlaner.Server.Services;

public class TodoService
{
    private readonly AppDbContext _context;

    public TodoService(AppDbContext context)
    {
        _context = context;
    }

    // TodoList Methods
    public async Task<List<TodoListDto>> GetTodoListsByHouseholdAsync(int householdId)
    {
        return await _context.TodoLists
  .Where(tl => tl.HouseholdId == householdId)
   .Include(tl => tl.CreatedBy)
     .Include(tl => tl.Items)
          .Select(tl => new TodoListDto
          {
              Id = tl.Id,
              Title = tl.Title,
              Description = tl.Description,
              CreatedAt = tl.CreatedAt,
              CreatedByName = tl.CreatedBy != null ? tl.CreatedBy.FullName : null,
              ItemCount = tl.Items.Count,
              CompletedItemCount = tl.Items.Count(i => i.IsCompleted)
          })
          .OrderByDescending(tl => tl.CreatedAt)
    .ToListAsync();
    }

    public async Task<TodoListDto?> GetTodoListByIdAsync(int id, int householdId)
    {
        var todoList = await _context.TodoLists
       .Where(tl => tl.Id == id && tl.HouseholdId == householdId)
       .Include(tl => tl.CreatedBy)
              .Include(tl => tl.Items)
      .FirstOrDefaultAsync();

        if (todoList == null) return null;

        return new TodoListDto
        {
            Id = todoList.Id,
            Title = todoList.Title,
            Description = todoList.Description,
            CreatedAt = todoList.CreatedAt,
            CreatedByName = todoList.CreatedBy?.FullName,
            ItemCount = todoList.Items.Count,
            CompletedItemCount = todoList.Items.Count(i => i.IsCompleted)
        };
    }

    public async Task<TodoListDto> CreateTodoListAsync(CreateTodoListRequest request, int householdId, int userId)
    {
        var todoList = new TodoList
        {
            Title = request.Title,
            Description = request.Description,
            HouseholdId = householdId,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.TodoLists.Add(todoList);
        await _context.SaveChangesAsync();

        var createdBy = await _context.Users.FindAsync(userId);

        return new TodoListDto
        {
            Id = todoList.Id,
            Title = todoList.Title,
            Description = todoList.Description,
            CreatedAt = todoList.CreatedAt,
            CreatedByName = createdBy?.FullName,
            ItemCount = 0,
            CompletedItemCount = 0
        };
    }

    public async Task<bool> UpdateTodoListAsync(UpdateTodoListRequest request, int householdId)
    {
        var todoList = await _context.TodoLists
       .FirstOrDefaultAsync(tl => tl.Id == request.Id && tl.HouseholdId == householdId);

        if (todoList == null) return false;

        todoList.Title = request.Title;
        todoList.Description = request.Description;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteTodoListAsync(int id, int householdId)
    {
        var todoList = await _context.TodoLists
   .FirstOrDefaultAsync(tl => tl.Id == id && tl.HouseholdId == householdId);

        if (todoList == null) return false;

        _context.TodoLists.Remove(todoList);
        await _context.SaveChangesAsync();
        return true;
    }

    // TodoItem Methods
    public async Task<List<TodoItemDto>> GetTodoItemsByListAsync(int todoListId, int householdId)
    {
        // Verify the list belongs to the household
        var listExists = await _context.TodoLists
  .AnyAsync(tl => tl.Id == todoListId && tl.HouseholdId == householdId);

        if (!listExists) return new List<TodoItemDto>();

        return await _context.TodoItems
            .Where(ti => ti.TodoListId == todoListId)
   .Include(ti => ti.AssignedTo)
        .Include(ti => ti.CompletedBy)
            .Select(ti => new TodoItemDto
            {
                Id = ti.Id,
                Title = ti.Title,
                Description = ti.Description,
                IsCompleted = ti.IsCompleted,
                DueDate = ti.DueDate,
                CreatedAt = ti.CreatedAt,
                CompletedAt = ti.CompletedAt,
                TodoListId = ti.TodoListId,
                AssignedToName = ti.AssignedTo != null ? ti.AssignedTo.FullName : null,
                AssignedToUserId = ti.AssignedToUserId,
                CompletedByName = ti.CompletedBy != null ? ti.CompletedBy.FullName : null
            })
            .OrderBy(ti => ti.IsCompleted)
 .ThenBy(ti => ti.DueDate)
     .ThenByDescending(ti => ti.CreatedAt)
            .ToListAsync();
    }

    public async Task<TodoItemDto> CreateTodoItemAsync(CreateTodoItemRequest request, int householdId)
    {
        // Verify the list belongs to the household
        var listExists = await _context.TodoLists
            .AnyAsync(tl => tl.Id == request.TodoListId && tl.HouseholdId == householdId);

        if (!listExists)
            throw new UnauthorizedAccessException("TodoList not found or access denied");

        var todoItem = new TodoItem
        {
            Title = request.Title,
            Description = request.Description,
            DueDate = request.DueDate,
            AssignedToUserId = request.AssignedToUserId,
            TodoListId = request.TodoListId,
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.TodoItems.Add(todoItem);
        await _context.SaveChangesAsync();

        var assignedTo = request.AssignedToUserId.HasValue
   ? await _context.Users.FindAsync(request.AssignedToUserId.Value)
            : null;

        return new TodoItemDto
        {
            Id = todoItem.Id,
            Title = todoItem.Title,
            Description = todoItem.Description,
            IsCompleted = todoItem.IsCompleted,
            DueDate = todoItem.DueDate,
            CreatedAt = todoItem.CreatedAt,
            TodoListId = todoItem.TodoListId,
            AssignedToName = assignedTo?.FullName,
            AssignedToUserId = assignedTo?.Id
        };
    }

    public async Task<bool> UpdateTodoItemAsync(UpdateTodoItemRequest request, int householdId, int userId)
    {
        var todoItem = await _context.TodoItems
            .Include(ti => ti.TodoList)
              .FirstOrDefaultAsync(ti => ti.Id == request.Id && ti.TodoList.HouseholdId == householdId);

        if (todoItem == null) return false;

        todoItem.Title = request.Title;
        todoItem.Description = request.Description;
        todoItem.DueDate = request.DueDate;
        todoItem.AssignedToUserId = request.AssignedToUserId;

        if (request.IsCompleted && !todoItem.IsCompleted)
        {
            todoItem.IsCompleted = true;
            todoItem.CompletedAt = DateTime.UtcNow;
            todoItem.CompletedByUserId = userId;
        }
        else if (!request.IsCompleted && todoItem.IsCompleted)
        {
            todoItem.IsCompleted = false;
            todoItem.CompletedAt = null;
            todoItem.CompletedByUserId = null;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleTodoItemAsync(int id, bool isCompleted, int householdId, int userId)
    {
        var todoItem = await _context.TodoItems
                .Include(ti => ti.TodoList)
      .FirstOrDefaultAsync(ti => ti.Id == id && ti.TodoList.HouseholdId == householdId);

        if (todoItem == null) return false;

        todoItem.IsCompleted = isCompleted;

        if (isCompleted)
        {
            todoItem.CompletedAt = DateTime.UtcNow;
            todoItem.CompletedByUserId = userId;
        }
        else
        {
            todoItem.CompletedAt = null;
            todoItem.CompletedByUserId = null;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteTodoItemAsync(int id, int householdId)
    {
        var todoItem = await _context.TodoItems
 .Include(ti => ti.TodoList)
            .FirstOrDefaultAsync(ti => ti.Id == id && ti.TodoList.HouseholdId == householdId);

        if (todoItem == null) return false;

        _context.TodoItems.Remove(todoItem);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<UserDto>> GetHouseholdMembersAsync(int householdId)
    {
        return await _context.Users
            .Where(u => u.HouseholdId == householdId)
         .Select(u => new UserDto
         {
             Id = u.Id,
             Username = u.Username,
             FullName = u.FullName,
             Email = u.Email,
             ProfileImageUrl = u.ProfileImageUrl
         })
   .OrderBy(u => u.FullName)
     .ToListAsync();
    }
}
