namespace IndieMoba.Core
{
    public interface ISimulationSystem
    {
        int SimulationOrder { get; }
        void SimulateTick(float deltaTime, int tick);
    }

    public static class SimulationOrder
    {
        public const int Input = 0;
        public const int Spawning = 50;
        public const int Actors = 100;
        public const int Abilities = 200;
        public const int Projectiles = 300;
        public const int Damage = 400;
        public const int State = 500;
    }
}
