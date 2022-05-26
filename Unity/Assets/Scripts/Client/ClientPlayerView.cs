using UnityEngine;

namespace Code.Client
{
    public class ClientPlayerView : MonoBehaviour, IPlayerView
    {
        [SerializeField] private TextMesh _name;
        private ClientPlayer _player;
        private Camera _mainCamera;

        public static ClientPlayerView Create(ClientPlayerView prefab, ClientPlayer player)
        {
            Quaternion rot = Quaternion.Euler(0f, player.Rotation, 0f);
            return null;
        }

        private void Update()
        {
            var vert = Input.GetAxis("Vertical");
            var horz = Input.GetAxis("Horizontal");
            var fire = Input.GetAxis("Fire1");
        }
        
        public void Destroy()
        {
            Destroy(gameObject);
        }
    }
}