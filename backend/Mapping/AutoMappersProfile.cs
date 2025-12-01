using AutoMapper;
using backend.Models.Domain;
using backend.Models.Dtos;

namespace backend.Mapping;

public class AutoMappersProfiles : Profile
{
  public AutoMappersProfiles()
  {
    CreateMap<Chat, ChatMetaDataDto>().ReverseMap();
    CreateMap<ChatMessage, FullMessageDto>().ReverseMap();
    CreateMap<ApiUser, EmailNewsletterDTO>().ReverseMap();
    CreateMap<ApiUser, ApiUserDto>().ReverseMap();
  }
}