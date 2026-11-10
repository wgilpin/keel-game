using System;
using System.Numerics;

namespace Keel.Sim.Arena;

public sealed class SpaceDustField
{
    public const float DefaultBoxSize = 20.0f;
    public const int DefaultParticleCount = 128;

    private readonly Vector3[] _basePositions;

    public float BoxSize { get; }
    public int ParticleCount => _basePositions.Length;

    public SpaceDustField(int particleCount = DefaultParticleCount, float boxSize = DefaultBoxSize, int seed = 1337)
    {
        if (particleCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(particleCount), "Particle count must be positive.");
        }

        if (boxSize <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(boxSize), "Box size must be positive.");
        }

        BoxSize = boxSize;
        _basePositions = new Vector3[particleCount];

        var rng = new Random(seed);
        float halfBox = boxSize * 0.5f;

        for (int i = 0; i < particleCount; i++)
        {
            float x = ((float)rng.NextDouble() * boxSize) - halfBox;
            float y = ((float)rng.NextDouble() * boxSize) - halfBox;
            float z = ((float)rng.NextDouble() * boxSize) - halfBox;
            _basePositions[i] = new Vector3(x, y, z);
        }
    }

    public void WrapToCameraBox(Vector3 cameraPosition, Span<Vector3> destination)
    {
        int count = Math.Min(_basePositions.Length, destination.Length);
        float halfBox = BoxSize * 0.5f;

        for (int i = 0; i < count; i++)
        {
            Vector3 basePos = _basePositions[i];
            float relX = WrapRelative(basePos.X - cameraPosition.X, halfBox, BoxSize);
            float relY = WrapRelative(basePos.Y - cameraPosition.Y, halfBox, BoxSize);
            float relZ = WrapRelative(basePos.Z - cameraPosition.Z, halfBox, BoxSize);

            destination[i] = new Vector3(
                cameraPosition.X + relX,
                cameraPosition.Y + relY,
                cameraPosition.Z + relZ);
        }
    }

    private static float WrapRelative(float rel, float halfBox, float boxSize)
    {
        float shifted = rel + halfBox;
        float rem = shifted % boxSize;
        if (rem < 0f)
        {
            rem += boxSize;
        }
        return rem - halfBox;
    }
}
