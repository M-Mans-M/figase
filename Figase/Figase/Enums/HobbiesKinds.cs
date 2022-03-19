using System;
using System.ComponentModel;

namespace Figase.Enums
{
    /// <summary>
    /// Интересы
    /// </summary>
    [Flags]
    public enum HobbiesKinds
    {
        [Description("Ничего не интересно")]
        None = 0,

        [Description("Офигевать от происходящего")]
        GetFreaked = 0x01,

        [Description("Играть в игры")]
        Games = 0x02,

        [Description("Смотреть как другие играют в игры")]
        WatchGames = 0x04,

        [Description("Готовить еду")]
        Cooking = 0x08,

        [Description("Смотреть как другие готовят еду")]
        WatchCooking = 0x10,

        [Description("Заниматься сексом")]
        Sex = 0x20,

        [Description("Смотреть как другие занимаются сексом")]
        WatchSex = 0x40,

        [Description("Работать")]
        Work = 0x80,

        [Description("Не работать")]
        NotWork = 0x100,

        [Description("Смотреть как другие работают")]
        WatchWork = 0x200,

        [Description("То-сё")]
        N1 = 0x400,

        [Description("Пятое-десятое")]
        N2 = 0x800,

        [Description("Туда-сюда")]
        N3 = 0x1000,

        [Description("Ну и всё такое")]
        N4 = 0x2000
    }
}
