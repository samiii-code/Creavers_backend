using AutoMapper;
using Creavers.API.DTOs.Categories;
using Creavers.API.Models;

namespace Creavers.API.Mappings
{
    public class CategoryMappingProfile : Profile
    {
        public CategoryMappingProfile()
        {
            CreateMap<Category, CategoryDto>();
            CreateMap<CreateCategoryRequest, Category>();
            CreateMap<UpdateCategoryRequest, Category>();
        }
    }
}
