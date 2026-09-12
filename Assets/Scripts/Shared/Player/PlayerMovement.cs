using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
   [SerializeField] private float moveSpeed = 5f;

   private Vector2 _movement;

   private Rigidbody2D _rb;

   private void Awake(){
    _rb = GetComponent<Rigidbody2D>();
   }

   private void Update(){
    _movement.Set(InputManager.Movement.x, InputManager.Movement.y);

    //_rb.velocity = _movement.normalized * moveSpeed;
    _rb.linearVelocity = _movement * moveSpeed;
   }
}
