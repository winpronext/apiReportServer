using System.Data.Entity;
using System.Linq;
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
    [RoutePrefix("api/TodoList")]
    public class TodoListController: ApiController
    {
        [Queryable]
        [HttpGet]
        public async Task<IHttpActionResult> Get()
        {
            Log.Debug("Entering Get()");

            var todoLists = await _repository.Query<TodoList>()
                .Where(td => td.UserId == User.Identity.Name)
                .ToArrayAsync();

            var result = Mapper.Map<TodoList[], TodoListViewModel[]>(todoLists);

            Log.DebugFormat("Leaving Get(): Count={0}", result.Length);

            return Ok(result);
        }

        [HttpGet, Route("{id:int}")]
        public async Task<IHttpActionResult> Get(int id)
        {
            Log.DebugFormat("Entering Get(id={0})", id);

            var todoList = await _repository.GetAsync<TodoList>(id);

            if (todoList == null)
            {
                Log.Debug("Leaving Get(): Not Found");
                return NotFound();
            }

            var result = Mapper.Map<TodoList, TodoListViewModel>(todoList);

            Log.DebugFormat("Leaving Get(): Id={0}", result.Id);

            return Ok(result);
        }

        [HttpPost]
        public async Task<IHttpActionResult> Post(TodoListViewModel model)
        {
            Log.DebugFormat("Entering Post(model.Title = {0})", model.Title);

            if (!ModelState.IsValid)
            {
                Log.Debug("Leaving Post(): Bad request");
                return this.BadRequestError(ModelState);
            }

            var todoList = Mapper.Map<TodoListViewModel, TodoList>(model);
            todoList.UserId = User.Identity.Name;

            _repository.Add(todoList);
            await _repository.SaveChangesAsync();
            
            var result = Mapper.Map<TodoList, TodoListViewModel>(todoList);

            Log.DebugFormat("Leaving Post(): Id={0}", result.Id);

            return Ok(result);
        }

        [Queryable]
        [HttpGet, Route("{id:int}/Todos")]
        public async Task<IHttpActionResult> Todos(int id)
        {
            Log.DebugFormat("Entering Todos(id={0})", id);

            var todoList = await _repository.GetAsync<TodoList>(id);

            if (todoList == null)
            {
                Log.Debug("Leaving Todos(): Not found");
                return NotFound();
            }

            if (todoList.UserId != User.Identity.Name)
            {
                Log.Debug("Leaving Todos(): Unauthorized");
                return Unauthorized();
            }

            var todos = await _repository.Query<TodoItem>()
                .Where(x => x.TodoListId == id)
                .ToArrayAsync();

            var result = Mapper.Map<TodoItem[], TodoItemViewModel[]>(todos);

            Log.DebugFormat("Leaving Todos(): Count={0}", result.Length);

            return Ok(result);
        }

        [HttpDelete, Route("{id:int}")]
        public async Task<IHttpActionResult> Delete(int id)
        {
            Log.DebugFormat("Entering Delete(id={0})", id);

            var todoList = _repository.Get<TodoList>(id);
            if (todoList == null)
            {
                Log.Debug("Leaving Delete(): Not found");
                return NotFound();
            }

            if (todoList.UserId != User.Identity.Name)
            {
                Log.Debug("Leaving Delete(): Unauthorized");
                return Unauthorized();
            }

            _repository.Remove<TodoList>(id);
            await _repository.SaveChangesAsync();

            Log.Debug("Leaving Delete()");
            return Ok();
        }

        public TodoListController(IAsyncRepository repository)
        {
            Log.Debug("Entering TodoListController()");
            _repository = repository;
        }

        protected override void Dispose(bool disposing)
        {
            Log.DebugFormat("Entering Dispose(disposing={0})", disposing);
            _repository.Dispose();
            base.Dispose(disposing);
        }

        private readonly IAsyncRepository _repository;
        private static readonly ILog Log = LogManager.GetLogger(typeof(TodoListController));
    }
}