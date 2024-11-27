using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CCApi.WebApiExample.Controllers;

public partial class BookController
{
    [HttpGet("additional")]
    public async Task<IActionResult> GetBooksOne()
    {
        return Ok("Hello Dule from partial class");
    }
    
    [HttpGet("authors")]
    public async Task<IActionResult> GetBooksTest()
    {
        var result = await dbContext.Books.Include(p => p.Author).ToListAsync();
        return Ok(result);
    }

    // [HttpPut("{id}")]
    // public async Task<IActionResult> Update(string id, Book model)
    // {
    //     var res = await dbContext.Books.FirstOrDefaultAsync(p => p.Id == id);
    //     if (res is null || model is null)
    //         return BadRequest();
    //
    //     res = model;
    //     await dbContext.SaveChangesAsync();
    //     return Ok();
    // }
    
    // [HttpPut("{id}")]
    // public async Task<IActionResult> Update(string id, Book model)
    // {
    //     if (model == null || string.IsNullOrWhiteSpace(id))
    //         return BadRequest("Invalid data provided.");
    //
    //     var existingBook = await dbContext.Books.FirstOrDefaultAsync(p => p.Id == id);
    //     if (existingBook == null)
    //         return NotFound($"Book with ID {id} not found.");
    //
    //     // Map incoming model to the existing entity
    //     model.Id = id; // Ensure the ID remains consistent
    //     dbContext.Entry(existingBook).CurrentValues.SetValues(model);
    //
    //     await dbContext.SaveChangesAsync();
    //     return Ok(existingBook);
    // }
}