namespace ChuckAI.Core.Models;

public class Joke
{
    public int Id { get; set; }
    public string JokeText { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
