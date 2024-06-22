using System;

namespace TheJazMaster.EnemyPack;

public interface IMoreDifficultiesApi
{
    Type BegCardType { get; }
	int Difficulty2 { get; }
}
