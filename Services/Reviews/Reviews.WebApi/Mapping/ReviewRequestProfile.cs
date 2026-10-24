using AutoMapper;
using Reviews.Application.Commands.CreateReview;
using Reviews.WebApi.Contracts;

namespace Reviews.WebApi.Mapping;

public class ReviewRequestProfile : Profile
{
    public ReviewRequestProfile()
    {
        // запит API → команда MediatR
        CreateMap<CreateReviewRequest, CreateReviewCommand>();
    }
}