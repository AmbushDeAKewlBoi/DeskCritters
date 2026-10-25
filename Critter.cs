using System;

namespace DeskCritters;

public enum CritterSpecies
{
    Slime,
    Cat,
    Dog,
    Dragon,
    Elf
}

public enum CritterPersonality
{
    Lazy,
    Normal,
    Energetic
}

public enum CritterState
{
    Walking,
    Resting
}

public class Critter
{
    private readonly Random random = new Random();
    private int ticksUntilChange;

    public string Name { get; set; }
    public CritterSpecies Species { get; private set; }
    public CritterPersonality Personality { get; private set; }
    public CritterState State { get; private set; }

    public double Speed { get; private set; }
    public int Direction { get; set; }

    public Critter(string name, CritterSpecies species)
    {
        Name = name;
        Direction = 1;

        Personality = (CritterPersonality)random.Next(3);

        ChangeSpecies(species);
        State = CritterState.Walking;

        ticksUntilChange = random.Next(30, 90);
    }

    public void ChangeSpecies(CritterSpecies species)
    {
        Species = species;

        switch (species)
        {
            case CritterSpecies.Slime:
                Speed = 1.5;
                break;

            case CritterSpecies.Cat:
                Speed = 2.5;
                break;

            case CritterSpecies.Dog:
                Speed = 3.0;
                break;

            case CritterSpecies.Dragon:
                Speed = 2.0;
                break;

            case CritterSpecies.Elf:
                Speed = 2.2;
                break;
        }
    }

    public bool Sprinting { get; private set; }

    public double CurrentSpeed
    {
        get
        {
            return Sprinting ? Speed * 1.8 : Speed;
        }
    }

    public void Update()
    {
        ticksUntilChange--;

        if (ticksUntilChange > 0)
        {
            return;
        }

        int walkChance = Personality switch
        {
            CritterPersonality.Lazy => 55,
            CritterPersonality.Normal => 75,
            CritterPersonality.Energetic => 90,
            _ => 75
        };

        bool shouldWalk = random.Next(100) < walkChance;

        if (shouldWalk)
        {
            State = CritterState.Walking;
            Direction = random.Next(2) == 0 ? -1 : 1;

            // Cats occasionally sprint.
            Sprinting =
                Species == CritterSpecies.Cat &&
                random.Next(100) < 25;

            ticksUntilChange = Personality switch
            {
                CritterPersonality.Lazy => random.Next(25, 65),
                CritterPersonality.Normal => random.Next(35, 100),
                CritterPersonality.Energetic => random.Next(50, 130),
                _ => random.Next(35, 100)
            };
        }
        else
        {
            State = CritterState.Resting;
            Sprinting = false;
            ticksUntilChange = random.Next(10, 35);
        }
    }

    public bool IsWalking()
    {
        return State == CritterState.Walking;
    }
}


