using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Reviews.Application.Commands.CreateReview;
using Reviews.Application.Common.Models;
using Reviews.Domain.Entities;
using Reviews.Application.Common.Mappings;

namespace Reviews.Application.Common.Mappings;

public class ReviewProfile : Profile
{
    public ReviewProfile()
    {
        // сутність → DTO: ObjectId → string, Value Objects, вкладений масив
        CreateMap<Review, ReviewDto>()
            .ForCtorParam(nameof(ReviewDto.Id), o => o.MapFrom(s => s.Id.ToString()))
            .ForCtorParam(nameof(ReviewDto.AuthorName), o => o.MapFrom(s => s.Author.DisplayName))
            .ForCtorParam(nameof(ReviewDto.Rating), o => o.MapFrom(s => s.Rating.Value))
            .ForCtorParam(nameof(ReviewDto.CommentsCount), o => o.MapFrom(s => s.Comments.Count));
    }
}