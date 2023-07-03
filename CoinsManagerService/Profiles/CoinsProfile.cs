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
            CreateMap<CoinCreateDto, Coin>();
            CreateMap<CoinUpdateDto, Coin>();            
        }
    }
}
