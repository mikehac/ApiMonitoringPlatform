namespace Watchtower.Domain.Exceptions;

public class NotFoundException : DomainException
{
    public NotFoundException(string entity, object key)
        : base($"{entity} '{key}' was not found.") { }
}
