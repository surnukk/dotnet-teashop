using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reviews.Application.Common.Interfaces;

namespace Reviews.Application.Commands.RemoveComment;

public record RemoveCommentCommand(string ReviewId, string CommentId) : ICommand;