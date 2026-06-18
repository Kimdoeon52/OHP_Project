public enum AudioEventId
{
    None = 0,

    MonsterHit,
    MonsterKilled,
    BossHit,
    BossDefeated,
    PlayerDamaged,
    PlayerDied,

    ItemPicked,
    GoldPicked,
    KeyPicked,
    ChestOpened,

    PotionPicked,
    PotionDrink,
    PotionThrow,
    PotionImpactFire,
    PotionImpactIce,

    StatusApplied,
    ButtonClicked,
    PortalEnter,

    MoveEquipment,
    MovePotion,
    MoveScroll,

    ScrollUsed,
    ScrollReinforce,
    ScrollPurify,
    ScrollAnalyze,
    ScrollMapReveal,

    BossA_Pattern1,
    BossA_Pattern2,
    BossA_Pattern3,
    BossB_Pattern1,
    BossB_Pattern2,
    BossB_Pattern3,
    LastBoss_HandSlam,
    LastBoss_Sweep,
    LastBoss_Scratch,
    LastBoss_HeadLaser,
    LastBoss_Meteor,

    DaggerThrow,

    ScrollPicked,
    EquipmentPicked,
    PlayerAttack
}
