using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reviews.Application.Common.Interfaces;

namespace Reviews.Application.Commands.AddComment;

/// <summary>Повертає Id доданого коментаря.</summary>
public record AddCommentCommand(
    string ReviewId,
    string AuthorId,
    string AuthorName,
    string Text) : ICommand<string>;