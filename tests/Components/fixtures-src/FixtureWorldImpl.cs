using FixtureWorld;

namespace FixtureWorld.exports;

public static class FixtureWorldImpl
{
    public static IFixtureWorld.Point Origin() => new(3, 4);

    public static uint[] Range() => new uint[] { 10, 20, 30 };

    public static IFixtureWorld.Priority TopPriority() => IFixtureWorld.Priority.HIGH;

    public static IFixtureWorld.Permissions Defaults() =>
        IFixtureWorld.Permissions.READ | IFixtureWorld.Permissions.WRITE;

    public static IFixtureWorld.Greeting Greet(bool formal) =>
        formal
            ? IFixtureWorld.Greeting.Formal("Sir")
            : IFixtureWorld.Greeting.Casual("hi");

    public static uint SafeDivide(uint n, uint d)
    {
        if (d == 0)
        {
            throw new WitException("division by zero", 0);
        }
        return n / d;
    }

    public static string? Find(uint needle) => needle == 42 ? "answer" : null;

    public static (uint, string) Pair() => (7, "seven");

    public static uint Square(uint n) => n * n;

    public static IFixtureWorld.Point Translate(IFixtureWorld.Point p, uint dx, uint dy) =>
        new(p.x + dx, p.y + dy);

    public static uint UseHost(uint n) => FixtureWorld.HostDouble(n);
}
