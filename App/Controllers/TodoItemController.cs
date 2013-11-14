using System.Threading.Tasks;
using System.Web.Http;
using App.Common;
using App.Models;
using App.ViewModels;
using AutoMapper;
using log4net;

namespace App.Controllers
{
    [Authorize]
    public class TodoItemController : ApiController
    {
        public async Task<IHttpActionResult> Post(TodoItemViewModel model)
        {
            Log.DebugFormat("Entering Post(model.Title = {0}), User: {1}", model.Title, User.Identity.Name);

            if (!ModelState.IsValid)
            {
                Log.Debug("Leaving Post(): Bad request error");
                return this.BadRequestError(ModelState);
            }

            var todoList = _repository.Get<TodoList>(model.TodoListId);

            if (todoList.UserId != User.Identity.Name)
            {
                Log.Debug("Leaving Post(): Unauthorized");
                return Unauthorized();
            }

            var todoItem = Mapper.Map<TodoItemViewModel, TodoItem>(model);

            todoList.Todos.Add(todoItem);

            await _repository.SaveChangesAsync();

            var entity = Mapper.Map<TodoItem, TodoItemViewModel>(todoItem);

            Log.DebugFormat("Leaving Post(): Id={0}", entity.Id);
            return Ok(entity);
        }

        public async Task<IHttpActionResult> Put(TodoItemViewModel model)
        {
            Log.DebugFormat("Entering Put(model.id={0})", model.Id);
            
            if (!ModelState.IsValid)
            {
                Log.Debug("Leaving Put(): Bad request");
                return this.BadRequestError(ModelState);
            }

            var todoList = _repository.Get<TodoList>(model.TodoListId);
            if (todoList.UserId != User.Identity.Name)
            {
                Log.Debug("Leaving Put(): Unauthorized");
                return Unauthorized();
            }

            var todoItem = Mapper.Map<TodoItemViewModel, TodoItem>(model);
            _repository.Update(todoItem);
            await _repository.SaveChangesAsync();

            var entity = Mapper.Map<TodoItem, TodoItemViewModel>(todoItem);
            
            Log.Debug("Leaving Put()");

            return Ok(entity);
        }

        public async Task<IHttpActionResult> Delete(int id)
        {
            Log.DebugFormat("Entering Delete(id={0})", id);

            var todoItem = _repository.Get<TodoItem>(id);
            if (todoItem == null)
            {
                Log.Debug("Leaving Delete(): Not found");
                return NotFound();
            }

            if (todoItem.TodoList.UserId != User.Identity.Name)
            {
                Log.Debug("Leaving Delete(): Unauthorized");
                return Unauthorized();
            }

            _repository.Remove<TodoItem>(id);
            await _repository.SaveChangesAsync();

            Log.Debug("Leaving Delete()");
            return Ok();
        }

        public TodoItemController(IAsyncRepository repository)
        {
            Log.Debug("Entering TodoItemController()");
            _repository = repository;
        }

        protected override void Dispose(bool disposing)
        {
            Log.DebugFormat("Entering Dispose(disposing={0})", disposing);
            _repository.Dispose();
            base.Dispose(disposing);
        }

        private readonly IAsyncRepository _repository;
        private static readonly ILog Log = LogManager.GetLogger(typeof(TodoItemController));
    }
}