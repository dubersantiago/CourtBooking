using Domain.Common;
using Domain.Reservations;

namespace Domain.Courts;

public class Court
{
    public const int MaxCourtImages = 5;
    private readonly List<CourtImage> _Images = new();
    public Guid Id { get; }
    public string Name { get; }
    public bool IsActive { get; private set; }
    public IReadOnlyCollection<CourtImage> Images => _Images.AsReadOnly();

    private Court()
    {
        Name = null!;
    }
    public Court(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("El Nombre de la cancha es obligatorio");

        Id = Guid.NewGuid();
        Name = name.Trim();
        IsActive = true;
    }

    public void Deactivate()
    {
        if (!IsActive) throw new DomainException("La cancha ya esta desactivada");
        IsActive = false;
    }

    public Reservation Reserve(Guid userId, TimeRange range, IEnumerable<Reservation> existingReservations)
    {
        ArgumentNullException.ThrowIfNull(existingReservations);

        if (!IsActive) throw new DomainException("La cancha no esta activa");

        var IsTaken = existingReservations.Any(res =>
            res.CourtId == Id &&
            range.Overlaps(res.TimeRange) &&
            res.Status == ReservationStatus.Confirmada);

        if (IsTaken) throw new DomainException("La cancha no esta disponible");

        return new Reservation(Id, userId, range);
    }

    public CourtImage AddCourtImage(string key)
    {
        if(_Images.Count >= MaxCourtImages) throw new DomainException($"Una cancha puede tener maximo {MaxCourtImages} imagenes");

        var image = new CourtImage(key);
        _Images.Add(image);
        return image;
    }

    public CourtImage RemoveCourtImage(Guid id)
    {
        var image = _Images.FirstOrDefault(i => i.Id ==id)
            ?? throw new DomainException("No existe la imagen");
        
        _Images.Remove(image);
        return image;
    }
}