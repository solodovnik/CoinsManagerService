using AutoMapper;
using CoinsManagerService.Dtos;
using CoinsManagerService.Models;

namespace CoinsManagerService.Profiles
{
    public class CoinsProfile : Profile
    {
        public CoinsProfile()
        {
            CreateMap<Coin, CoinReadDto>();
            CreateMap<Coin, CoinUpdateDto>();
            CreateMap<CoinCreateDto, Coin>()
                .ForSourceMember(x => x.ObverseImage, y => y.DoNotValidate())
                .ForSourceMember(x => x.ReverseImage, y => y.DoNotValidate());
            CreateMap<CoinUpdateDto, Coin>();            
        }
    }
}
