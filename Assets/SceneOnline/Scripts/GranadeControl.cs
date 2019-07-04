using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GranadeControl : Photon.MonoBehaviour
{

	private Vector3 _currentPosition;
	private Quaternion _currentRotation;
	private bool _firstTake;

	private float _countDown = 5;

	[SerializeField]
	private int _playerID;

	// Use this for initialization
	void Start()
	{
		_playerID = GetComponent<PhotonView>().ownerId;
	}

	// Update is called once per frame
	void Update()
	{
		if (photonView.isMine)
		{
			_countDown -= Time.deltaTime;
			//Cuenta atrás antes de explotar
			if (_countDown <= 0)
			{
				//Explota y localiza los players dentro del area
				Collider[] allDetect = Physics.OverlapSphere(transform.position, 5);
				for (int i = 0; i < allDetect.Length; i++)
				{
					if (allDetect[i].tag == "Player")
					{
						//Calcula la distancia entre la granada y los players dentro
						float distancePlayer = Vector3.Distance(transform.position, allDetect[i].transform.position);
						if (distancePlayer < 1)
						{
							distancePlayer = 1;
						}

						//Les hace daño en función a la distancia
						allDetect[i].GetComponent<PhotonView>().RPC("GetDamage", PhotonTargets.All, (int)(100 / distancePlayer), _playerID);

					}
				}

				PhotonNetwork.Destroy(gameObject);
			}
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
