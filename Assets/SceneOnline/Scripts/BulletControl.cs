using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletControl : Photon.MonoBehaviour
{

	private Vector3 _currentPosition;
	private Quaternion _currentRotation;
	private bool _firstTake;

	private float _speed = 15;

	public int playerID;

	private void OnEnable()
	{
		playerID = GetComponent<PhotonView>().ownerId;
	}

	// Update is called once per frame
	void Update()
	{
		if (photonView.isMine)
		{
			transform.Translate(Vector3.forward * _speed * Time.deltaTime);
		}
		else
		{
			transform.position = Vector3.Lerp(transform.position, _currentPosition, 5 * Time.deltaTime);
			transform.rotation = Quaternion.Lerp(transform.rotation, _currentRotation, 5 * Time.deltaTime);
		}
	}

	void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (stream.isWriting)
		{
			stream.SendNext(transform.position);
			stream.SendNext(transform.rotation);
		}
		else
		{
			_currentPosition = (Vector3)stream.ReceiveNext();
			_currentRotation = (Quaternion)stream.ReceiveNext();
			if (_firstTake == false)
			{
				transform.position = _currentPosition;
				transform.rotation = _currentRotation;
				_firstTake = true;
			}
		}
	}
}
