using HitstunConstants;

[System.Serializable]
public class CharacterNode
{
    public int Id;
    public string Name;
    public int Health;
    public int Stun;
    public int LP;
    public int MP;
    public int HP;
    public int LK;
    public int MK;
    public int HK;
    public int EX1;
    public int EX2;
    public int EX3;

    public int GetDamage(CharacterState state)
    {
        switch (state)
        {
            case CharacterState.STAND_LP:
            case CharacterState.CROUCH_LP:
                return LP;
            case CharacterState.STAND_MP:
            case CharacterState.CROUCH_MP:
                return MP;
            case CharacterState.STAND_HP:
            case CharacterState.CROUCH_HP:
                return HP;
            case CharacterState.STAND_LK:
            case CharacterState.CROUCH_LK:
                return LK;
            case CharacterState.STAND_MK:
            case CharacterState.CROUCH_MK:
                return MK;
            case CharacterState.STAND_HK:
            case CharacterState.CROUCH_HK:
                return HK;
            case CharacterState.HADOUKEN:
                return EX1;
            case CharacterState.SHORYUKEN:
                return EX2;
            default:
                return 0;
        }
    }
}