using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Conduit.Infrastructure;
using Conduit.Infrastructure.Errors;
using Conduit.Infrastructure.Security;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Conduit.Features.Users;

public class Login
{
    public class UserData
    {
        public string? Email { get; init; }

        public string? Username { get; init; }

        public string? Password { get; init; }
    }

    public record Command(UserData User) : IRequest<UserEnvelope>;

    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            RuleFor(x => x.User).NotNull();

            When(
                x => x.User is not null,
                () =>
                {
                    RuleFor(x => x.User.Password).NotNull().NotEmpty();

                    RuleFor(x => x.User)
                        .Must(user =>
                            user is not null
                            && (
                                !string.IsNullOrWhiteSpace(user.Email)
                                || !string.IsNullOrWhiteSpace(user.Username)
                            )
                        )
                        .WithMessage("Either email or username is required.");
                }
            );
        }
    }

    public class Handler(
        ConduitContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IMapper mapper
    ) : IRequestHandler<Command, UserEnvelope>
    {
        public async Task<UserEnvelope> Handle(Command message, CancellationToken cancellationToken)
        {
            var login = !string.IsNullOrWhiteSpace(message.User.Username)
                ? message.User.Username.Trim()
                : message.User.Email?.Trim();

            if (string.IsNullOrWhiteSpace(login))
            {
                throw new RestException(
                    HttpStatusCode.BadRequest,
                    new { Error = "Either email or username is required." }
                );
            }

            var person = await context
                .Persons
                .Where(x => x.Email == login || x.Username == login)
                .FirstOrDefaultAsync(cancellationToken);

            if (person == null)
            {
                throw new RestException(
                    HttpStatusCode.Unauthorized,
                    new { Error = "Invalid username/email / password." }
                );
            }

            var hash = await passwordHasher.Hash(
                message.User.Password ?? throw new InvalidOperationException(),
                person.Salt
            );

            if (!person.Hash.SequenceEqual(hash))
            {
                throw new RestException(
                    HttpStatusCode.Unauthorized,
                    new { Error = "Invalid username/email / password." }
                );
            }

            var user = mapper.Map<Domain.Person, User>(person);
            user.Token = jwtTokenGenerator.CreateToken(
                person.Username ?? throw new InvalidOperationException()
            );

            return new UserEnvelope(user);
        }
    }
}
