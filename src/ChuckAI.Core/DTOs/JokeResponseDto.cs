namespace ChuckAI.Core.DTOs;

public class JokeResponseDto
{
    public int Id { get; set; }
    public string Joke { get; set; } = string.Empty;
    public string? Url { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
