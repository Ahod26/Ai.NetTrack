public class RestDTO<T>
{
  public List<LinkDTO> Links { get; set; } = new List<LinkDTO>();
  public T Data { get; set; } = default!;
  public bool IsSuccess { get; set; } = true;
  public string? Message { get; set; }
  public List<string>? Errors { get; set; }

}