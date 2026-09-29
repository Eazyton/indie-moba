namespace IndieMoba.Core
{
    public interface IMovementInputSource
    {
        MovementIntent ReadIntent();
    }
}
