using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private Rigidbody rb;
    [SerializeField] private FixedJoystick _joystick;
    [SerializeField] private Animator _anim;


    [SerializeField] private float _moveSpeed;

    private void Update()
    {
        //rb.velocity = new Vector3(_joystick.Horizontal * _moveSpeed, rb.velocity.y, _joystick.Vertical * _moveSpeed);
        rb.velocity = new Vector3(Input.GetAxis("Horizontal") * _moveSpeed, rb.velocity.y, Input.GetAxis("Vertical") * _moveSpeed);

        if (Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0)
        {
            transform.rotation = Quaternion.LookRotation(rb.velocity);

            if (!_anim.GetBool("isRunning"))
                _anim.SetBool("isRunning", true);
        }
        else
        {
            if (_anim.GetBool("isRunning"))
                _anim.SetBool("isRunning", false);
        }
    }
}
