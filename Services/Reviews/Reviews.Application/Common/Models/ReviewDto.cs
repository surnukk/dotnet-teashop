using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Reviews.Application.Common.Models;

public record ReviewDto(
    string Id,
    long ProductId,
    string ProductName,
    string AuthorName,
    int Rating,
    string Text,
    int CommentsCount,
    int Version);