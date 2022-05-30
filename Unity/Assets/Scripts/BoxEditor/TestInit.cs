using UnityEngine;
using Newtonsoft.Json;
using HitstunConstants;

public class TestInit : MonoBehaviour
{
    public CharacterView characterView;
    CharacterData characterData;
    public Character role;

    public CharacterState state;

    void Start()
    {
        var ta1 = Resources.Load<TextAsset>($"CharacterData/KEN");
        characterData = JsonConvert.DeserializeObject<CharacterData>(ta1.text);

        characterView.LoadResources(characterData);
        characterView.showHitboxes = true;

        role = new Character();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (role.state != state)
            {
                role.state = state;
                role.framesInState = 0;
                role.hitBoxes.Clear();

                if (characterData.attacks.ContainsKey(state.ToString()) == false) return;
                Debug.Log("" + state.ToString() + ": " + characterData.attacks.Count);
                foreach (var item in characterData.attacks)
                {
                    Debug.Log(item.Key + ": " + item.Value.animationName);
                }


                foreach (HitBox hb in characterData.attacks[state.ToString()].hitBoxes)
                {
                    HitBox hitBox = new HitBox(hb);
                    hitBox.enabled = false;
                    hitBox.used = false;
                    role.hitBoxes.Add(hitBox);
                }
            }
        }

        role.framesInState++;
        foreach (HitBox hitBox in role.hitBoxes)
        {
            hitBox.enabled = hitBox.startingFrame <= role.framesInState && hitBox.startingFrame + hitBox.duration >= role.framesInState;
        }

        characterView.UpdateCharacterView(role);
    }
}