using UnityEngine;

namespace Code.Client
{
    public class RemotePlayerView : MonoBehaviour, IPlayerView
    {
        private RemotePlayer _player;

        public static RemotePlayerView Create(RemotePlayerView prefab, RemotePlayer player)
        {
            Quaternion rot = Quaternion.Euler(0f, player.Rotation, 0f);
            //var obj = Instantiate(prefab, player.Position, rot);
            //obj._player = player;
            return null;
        }

        private void Update()
        {
        }

        public void Destroy()
        {
            Destroy(gameObject);
        }
    }
}