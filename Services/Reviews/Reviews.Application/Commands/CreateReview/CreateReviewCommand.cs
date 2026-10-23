using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reviews.Application.Common.Interfaces;

namespace Reviews.Application.Commands.CreateReview;

public record CreateReviewCommand(
    long ProductId,
    string ProductName,
    string AuthorId,
    string AuthorName,
    int Rating,
    string Text) : ICommand<string>;