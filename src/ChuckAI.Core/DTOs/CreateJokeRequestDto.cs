namespace ChuckAI.Core.DTOs;

public class CreateJokeRequestDto
{
    public string Joke { get; set; } = string.Empty;
    public string? Url { get; set; }
}
