using System.ComponentModel;

namespace Figase.Enums
{
    /// <summary>
    /// Пол
    /// </summary>
    public enum GenderTypes
    {
        [Description("Ещё не определился")]
        NotFiguredYet = 0,

        [Description("Мужчина")]
        Male = 1,
        
        [Description("Женщина")]
        Female = 2,

        [Description("Не мужчина")]
        NotMale = 3,

        [Description("Не женщина")]
        NotFemale = 4,

        [Description("Мужчина считающий себя женщиной")]
        MaleThinkItsFemale = 5,

        [Description("Женщина считающая себя мужчиной")]
        FemaleThinkItsMale = 6,

        [Description("Мужчина который был женщиной")]
        MaleWasFemale = 7,

        [Description("Женщина которая была мужчиной")]
        FemaleWasMale = 8,

        [Description("Штурмовой вертолёт")]
        AttackHelicopter = 9,

        [Description("Андроид")]
        Android = 10,

        [Description("Яблоко")]
        Apple = 11,

        [Description("Эндергендер")]
        EnderGender = 12,

        [Description("Андромедагендер")]
        AndromedaGender = 13,

        [Description("Трансформер")]
        Transformer = 14,

        [Description("Трансформатор")]
        Transformator = 15,

        [Description("Транспоратор")]
        Transporator = 16,

        [Description("Смурф")]
        Smurf = 17,

        [Description("Иное сказочное существо")]
        Another = 99
    }
}
