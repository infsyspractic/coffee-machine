using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("NotesInMemoryDb"));

builder.Services.AddScoped<INoteService, NoteService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Notes API v1");
    c.RoutePrefix = string.Empty; // Открывает Swagger сразу по адресу http://localhost:XXXX/
});

app.UseAuthorization();
app.MapControllers();

app.Run();

namespace WebApiNotes.Models
{
    public class Note
    {
        [Key] 
        public int Id { get; set; }

        [Required(ErrorMessage = "Заголовок заметки обязателен для заполнения")]
        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;
    }
}


namespace WebApiNotes.Data
{
    using WebApiNotes.Models;

    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Таблица заметок в нашей виртуальной БД
        public DbSet<Note> Notes => Set<Note>();
    }
}

namespace WebApiNotes.Services
{
    using Microsoft.EntityFrameworkCore;
    using WebApiNotes.Data;
    using WebApiNotes.Models;

    public interface INoteService
    {
        Task<IEnumerable<Note>> GetAllNotesAsync();
        Task<Note?> GetNoteByIdAsync(int id);
        Task<Note> CreateNoteAsync(Note note);
    }

    public class NoteService : INoteService
    {
        private readonly AppDbContext _context;

        public NoteService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Note>> GetAllNotesAsync()
        {
            return await _context.Notes.ToListAsync();
        }

        public async Task<Note?> GetNoteByIdAsync(int id)
        {
            return await _context.Notes.FirstOrDefaultAsync(n => n.Id == id);
        }

        public async Task<Note> CreateNoteAsync(Note note)
        {
            _context.Notes.Add(note);
            await _context.SaveChangesAsync(); 
            return note;
        }
    }
}


namespace WebApiNotes.Controllers
{
    using WebApiNotes.Models;
    using WebApiNotes.Services;

    [ApiController]
    [Route("api/[controller]")] // Формирует маршрут /api/notes
    public class NotesController : ControllerBase
    {
        private readonly INoteService _noteService;

        public NotesController(INoteService noteService)
        {
            _noteService = noteService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Note>>> GetNotes()
        {
            var notes = await _noteService.GetAllNotesAsync();
            return Ok(notes);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Note>> GetNote(int id)
        {
            var note = await _noteService.GetNoteByIdAsync(id);
            if (note == null)
            {
                return NotFound(new { Message = $"Заметка с ID {id} не найдена." });
            }
            return Ok(note);
        }

        [HttpPost]
        public async Task<ActionResult<Note>> CreateNote([FromBody] Note note)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var createdNote = await _noteService.CreateNoteAsync(note);
            
            return CreatedAtAction(nameof(GetNote), new { id = createdNote.Id }, createdNote);
        }
    }
}