using AutoMapper;

public class AutoMappersProfiles : Profile
{
  public AutoMappersProfiles()
  {
    CreateMap<Chat, ChatMetaDataDto>().ReverseMap();
    CreateMap<ChatMessage, FullMessageDto>().ReverseMap();
  }
}