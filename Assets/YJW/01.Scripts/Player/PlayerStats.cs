using System;
using UnityEngine;

public enum StatType
{
    Hp,
    Strength,
    MoveSpeed,
    AttackSpeed,
    Defense,
    Level
}

public class PlayerStats : PersistentSingleton<PlayerStats>
{
    private int maxHp = 100;
    private int maxStrength = 99;
    private int maxMoveSpeed = 99;
    private int maxAttackSpeed = 99;
    private int maxDefense = 99;
    private int maxLevel = 100;

    private int _hp;
    private int _strength;
    private int _moveSpeed;
    private int _attackSpeed;
    private int _defense;
    private int _level;


    public event Action<StatType, int> OnStatChanged; // ½ºÅÈ º¯°æ ÀÌº¥Æ®

    public int Hp
    {
        get => _hp;
        set => _hp = Mathf.Clamp(value, 0, maxHp);
    }
    public int Strength
    {
        get => _strength;
        set => _strength = Mathf.Clamp(value, 0, maxStrength);
    }
    public int MoveSpeed
    {
        get => _moveSpeed;
        set => _moveSpeed = Mathf.Clamp(value, 0, maxMoveSpeed);
    }
    public int AttackSpeed
    {
        get => _attackSpeed;
        set => _attackSpeed = Mathf.Clamp(value, 0, maxAttackSpeed);
    }
    public int Defense
    {
        get => _defense;
        set => _defense = Mathf.Clamp(value, 0, maxDefense);
    }

    public void Reset()
    {
        Hp = maxHp;
        Strength = 10;
        MoveSpeed = 5;
        AttackSpeed = 5;
        Defense = 0;
    }

    public int Level
    {
        get => _level;
        set => _level = Mathf.Clamp(value, 0, maxLevel);
    }

    public void AddStat(StatType type, int amount)
    {
        switch (type)
        {
            case StatType.Hp: Hp += amount; break;
            case StatType.Strength: Strength += amount; break;
            case StatType.MoveSpeed: MoveSpeed += amount; break;
            case StatType.AttackSpeed: AttackSpeed += amount; break;
            case StatType.Defense: Defense += amount; break;
        }

        OnStatChanged?.Invoke(type, amount); // ½ºÅÈ ¹Ù²î¸é ¾Ë¸²
    }
}