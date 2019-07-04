using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class PlayerControl : Photon.MonoBehaviour
{
	public enum ShootType { ShootObj, Granade, Knife, Kick, Dead }
	public ShootType Type;

	private CharacterController _control;
	private Vector3 _moveDir = Vector3.zero;

	[SerializeField]
	private float _gravity = 24, _jumpForce = 8, _speedRotation = 5;

	[SerializeField]
	private float _rotY, _rotX;

	public float SpeedMove = 10;

	private Vector3 _currentPosition;
	private Quaternion _currentRotation;

	private GameObject _mainCamera;

	private RaycastHit _hitPlayer;

	[SerializeField]
	private int _life = 100;
	
	private GameObject _shootPoint;

	[SerializeField]
	private GameObject _bullet, _granade;

	private float _forceGranade;

	[SerializeField]
	private int _money;

	[SerializeField]
	private int _idDamage, _idDamageAll;
	private int _countAdd;

	private int _knifeDistance = 6;
	private int _kickDistance = 3;

	[SerializeField]
	private bool _allowAttack = true, _activeGranade = true;

	private GameObject[] _players;

	private Canvas _canvas;
	private Text _moneyText, _lifeText, _gameoverText;

	[SerializeField]
	private bool _move = true;

	[SerializeField]
	private Animator _anim;

	[SerializeField]
	private GameObject _currentGun, _goKnife, _goGranade;

	// Use this for initialization
	void Start()
	{
		_canvas = FindObjectOfType<Canvas>();

		_moneyText = _canvas.transform.GetChild(0).GetComponent<Text>();
		_lifeText = _canvas.transform.GetChild(1).GetComponent<Text>();

		_mainCamera = Camera.main.gameObject;

		_control = GetComponent<CharacterController>();
		
		GetComponent<PhotonView>().RPC("UpdateLife", PhotonTargets.All);

		//Ajustes de la cámara y del ShootPoint
		if (photonView.isMine)
		{
			_mainCamera.transform.SetParent(transform.Find("PosCamera"));
			_mainCamera.transform.localPosition = Vector3.zero;
			_mainCamera.transform.localRotation = Quaternion.Euler(Vector3.zero);

			_shootPoint = new GameObject();
			_shootPoint.transform.name = "ShootPoint";
			_shootPoint.transform.SetParent(Camera.main.transform);
			_shootPoint.transform.localPosition = new Vector3(0.3f, -0.3f, 0.7f);
			_shootPoint.transform.localRotation = Quaternion.Euler(Vector3.zero);
		}
	}

	// Update is called once per frame
	void Update()
	{
		_players = GameObject.FindGameObjectsWithTag("Player");

		if (photonView.isMine)
		{
			//Controles para usar los ataques cambiando la máquina de estado
			if (Input.GetKeyDown(KeyCode.LeftShift))
			{
				ChangeState(ShootType.Granade);
			}

			if (Input.GetKeyDown(KeyCode.E))
			{
				ChangeState(ShootType.Knife);
			}

			if (Input.GetKeyDown(KeyCode.Q))
			{
				ChangeState(ShootType.Kick);
			}

			switch (Type)
			{
				case ShootType.ShootObj:
					if (_currentGun.activeSelf == false)
					{
						_currentGun.SetActive(true);
					}

					//Cuando mantenemos pulsado va disparando
					if (Input.GetMouseButtonDown(0))
					{
						if (_allowAttack == true)
						{
							GameObject newBullet = PhotonNetwork.Instantiate(_bullet.name, _shootPoint.transform.position, _shootPoint.transform.rotation, 0);

							//Animacion disparo
							GetComponent<PhotonView>().RPC("ShootAnim", PhotonTargets.All, true);

							_allowAttack = false;

							StartCoroutine(DestroyObjectDelay(newBullet, 3));
							//Delay establecido para disparar cada 0.3f
							StartCoroutine(DelayAttack(0.3f));
						}
					} //Cuando soltamos la animacion se vuelve false
					else if (Input.GetMouseButtonUp(0))
					{
						//Animacion disparo
						GetComponent<PhotonView>().RPC("ShootAnim", PhotonTargets.All, false);
					}
					break;

				case ShootType.Granade:
					//Mientras mantenemos presionado, cargamos la distancia de la granada con una fuerza
					if (Input.GetKey(KeyCode.LeftShift))
					{
						if (_activeGranade == true)
						{
							_currentGun.SetActive(false);
							//Activamos el gameObject de la granada en la mano
							_goGranade.SetActive(true);
							_forceGranade = Mathf.MoveTowards(_forceGranade, 1500, 350 * Time.deltaTime);

							//Animacion cargar granada
							GetComponent<PhotonView>().RPC("GranadeAnim", PhotonTargets.All, 0);
						}
					} //Al soltar
					else if (Input.GetKeyUp(KeyCode.LeftShift))
					{
						_activeGranade = false;
						//Desactivamos esa granada de la mano para lanzar la otra
						_goGranade.SetActive(false);
						//Instanciamos la granada hacia delante
						GameObject _newGranade = PhotonNetwork.Instantiate(_granade.name, _shootPoint.transform.position, _shootPoint.transform.rotation, 0);
						//Y le añadimos la fuerza anterior cargada
						_newGranade.GetComponent<Rigidbody>().AddForce(_shootPoint.transform.forward * _forceGranade);
						_forceGranade = 0;
						//Animacion soltar granada
						GetComponent<PhotonView>().RPC("GranadeAnim", PhotonTargets.All, 1);

						//Delay para cambiar a disparar
						StartCoroutine(DelayChangeGranade(0.5f));
					}
					break;

				case ShootType.Knife:
					//Al mantener pulsado lanza un rayo si está a menos de KnifeDistance de distancia le acuchilla
					if (Input.GetKey(KeyCode.E))
					{
						if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out _hitPlayer, _knifeDistance))
						{
							if (_hitPlayer.collider.tag == "Player" && _hitPlayer.collider.gameObject != gameObject)
							{
								if (_allowAttack == true)
								{
									if (_goKnife.activeSelf == false)
									{
										print("entro");
										_currentGun.SetActive(false);
										_goKnife.SetActive(true);
									}

									//Paso mi ID como atacante
									_idDamage = GetComponent<PhotonView>().ownerId;

									//Animacion cuchillo
									GetComponent<PhotonView>().RPC("KnifeAnim", PhotonTargets.All, true); 

									//Resto vida, primer parámetro daño recibido, segundo parámetro el player que le ha golpeado
									_hitPlayer.collider.gameObject.GetComponent<PhotonView>().RPC("GetDamage", PhotonTargets.All, 15, _idDamage);
									_allowAttack = false;
									
									//Delay para volver a acuchillar
									StartCoroutine(DelayAttack(1.5f));
								}
							}
						}
					} //Suelto y todo se cancela
					else if (Input.GetKeyUp(KeyCode.E))
					{
						_goKnife.SetActive(false);
						GetComponent<PhotonView>().RPC("KnifeAnim", PhotonTargets.All, false); 

						//Cambiamos automáticamente a seguir disparando
						ChangeState(ShootType.ShootObj);
					}
					break;

				case ShootType.Kick:
					//Mantengo presionado para atacar con el cuchillo
					if (Input.GetKey(KeyCode.Q))
					{
						if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out _hitPlayer, _kickDistance))
						{
							if (_hitPlayer.collider.tag == "Player" && _hitPlayer.collider.gameObject != gameObject)
							{
								if (_allowAttack == true)
								{
									print("Ataco");
									//Paso mi ID como atacante
									_idDamage = GetComponent<PhotonView>().ownerId;

									//Animacion golpe
									GetComponent<PhotonView>().RPC("KickAnim", PhotonTargets.All, true);

									//Resto vida, primer parámetro daño recibido, segundo parámetro el player que le ha golpeado
									_hitPlayer.collider.gameObject.GetComponent<PhotonView>().RPC("GetDamage", PhotonTargets.All, 7, _idDamage);
									_allowAttack = false;
									
									StartCoroutine(DelayAttack(1f));
								}
							}
						}
					} //Al soltar 
					else if (Input.GetKeyUp(KeyCode.Q))
					{
						//Cambiamos automáticamente a seguir disparando
						ChangeState(ShootType.ShootObj);
					}
					break;

				case ShootType.Dead:

					//Animacion muerte
					GetComponent<PhotonView>().RPC("DeadAnim", PhotonTargets.All, true);
					break;
			}

			//Movimiento
			if (_control.isGrounded)
			{
				if (_move)
				{
					_moveDir = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
					_moveDir *= SpeedMove;
					_moveDir = transform.TransformDirection(_moveDir);

					if (Input.GetButton("Jump"))
					{
						_moveDir.y = _jumpForce;
					}

					//Animacion movimiento
					GetComponent<PhotonView>().RPC("MovementAnim", PhotonTargets.All);
					GetComponent<PhotonView>().RPC("JumpAnim", PhotonTargets.All, false);

				}
			}
			else
			{
				//Animacion salto
				GetComponent<PhotonView>().RPC("JumpAnim", PhotonTargets.All, true);
			}

			_rotX += Input.GetAxis("Mouse X") * _speedRotation;
			_rotY += Input.GetAxis("Mouse Y") * _speedRotation;

			_rotY = Mathf.Clamp(_rotY, -35, 90);

			transform.rotation = Quaternion.Euler(0, _rotX, 0);
			_mainCamera.transform.localEulerAngles = new Vector3(-_rotY, 0, 0);

			_moveDir.y -= _gravity * Time.deltaTime;
			_control.Move(_moveDir * Time.deltaTime);

		}
		else
		{
			transform.position = Vector3.Lerp(transform.position, _currentPosition, 5 * Time.deltaTime);
			transform.rotation = Quaternion.Lerp(transform.rotation, _currentRotation, 5 * Time.deltaTime);
		}
	}

	//Animacion movimiento
	[PunRPC]
	public void MovementAnim()
	{
		if (photonView.isMine)
		{
			print("playerlocal " + GetComponent<PhotonView>().ownerId.ToString());
			_anim.SetFloat("Speed", Mathf.Abs(Input.GetAxis("Horizontal")) + Mathf.Abs(Input.GetAxis("Vertical")));
		}
	}

	//Animacion salto
	[PunRPC]
	public void JumpAnim(bool active)
	{
		if (photonView.isMine)
		{
			_anim.SetBool("Jump", active);
		}
	}

	//Animacion disparo
	[PunRPC]
	public void ShootAnim(bool active)
	{
		if (photonView.isMine)
		{
			_anim.SetBool("Shoot", active);
		}
	}

	//Animacion cargar granada, status 0 es cargar granada, status 1 lanzar granada
	[PunRPC]
	public void GranadeAnim(int status)
	{
		if (photonView.isMine)
		{
			if (status == 0)
			{
				_anim.SetBool("Charge", true);
				_anim.SetBool("Granade", false);
			}
			else if (status == 1)
			{
				_anim.SetBool("Granade", true);
				_anim.SetBool("Charge", false);
			}
		}
	}

	//Animacion cuchillo
	[PunRPC]
	public void KnifeAnim(bool active)
	{
		print("Entro en animacion");
		_anim.SetBool("Knife", active);
	}

	//Animacion patada
	[PunRPC]
	public void KickAnim(bool active)
	{
		_anim.SetBool("Kick", active);
	}

	//Animacion muerte
	[PunRPC]
	public void DeadAnim(bool active)
	{
		_anim.SetBool("Dead", active);
	}

	/// <summary>
	/// Delay para destruir un objeto
	/// </summary>
	/// <param name="obj">Objeto a destruir</param>
	/// <param name="t">Tiempo de espera</param>
	/// <returns></returns>
	IEnumerator DestroyObjectDelay(GameObject obj, float t)
	{
		yield return new WaitForSeconds(t);
		PhotonNetwork.Destroy(obj);
	}

	/// <summary>
	/// Delay para desactivar animacion y volver a atacar
	/// </summary>
	/// <param name="t">Tiempo de espera</param>
	/// <returns></returns>
	IEnumerator DelayAttack(float t)
	{
		yield return new WaitForSeconds(t);

		GetComponent<PhotonView>().RPC("KickAnim", PhotonTargets.All, false);
		_allowAttack = true;
	}

	/// <summary>
	/// Delay para volver a disparar y activar la granada
	/// </summary>
	/// <param name="t">Tiempo de espera</param>
	/// <returns></returns>
	IEnumerator DelayChangeGranade(float t)
	{
		yield return new WaitForSeconds(t);

		_activeGranade = true;
		ChangeState(ShootType.ShootObj);
	}

	/// <summary>
	/// Función para actualizar el daño recibido, lanzándose dentro la de conseguir dinero si muere el personaje
	/// </summary>
	/// <param name="Damage">El daño que recibe</param>
	/// <param name="idDmg">El player que le ha hecho daño</param>
	[PunRPC]
	public void GetDamage(int Damage, int idDmg)
	{
		_life -= Damage;

		if (_life <= 0)
		{
			_life = 0;
			_idDamageAll = idDmg;
			GetComponent<PhotonView>().RPC("GetMoney", PhotonTargets.Others, _idDamageAll);
			ChangeState(ShootType.Dead);
			//El player ha muerto (Respawn)
			_move = false;
			GetComponent<CharacterController>().enabled = false;
			StartCoroutine(DelayRespawn(5));
		}

		GetComponent<PhotonView>().RPC("UpdateLife", PhotonTargets.All);
	}

	//Iniciando de nuevo al player
	IEnumerator DelayRespawn(float t)
	{
		yield return new WaitForSeconds(t);

		transform.position = new Vector3(Random.Range(-20f, 20f), transform.position.y + 4f, Random.Range(-20f, 20f));
		GetComponent<PhotonView>().RPC("DeadAnim", PhotonTargets.All, false);
		GetComponent<CharacterController>().enabled = true;
		ChangeState(ShootType.ShootObj);
		_move = true;
		_life = 100;
		GetComponent<PhotonView>().RPC("UpdateLife", PhotonTargets.All);
	}

	/// <summary>
	/// Función para asignar dinero a un player
	/// </summary>
	/// <param name="id"> id del jugador al que sumarle el dinero</param>
	[PunRPC]
	public void GetMoney(int id)
	{
		foreach (var player in _players)
		{
			if (id == player.GetComponent<PhotonView>().ownerId)
			{
				if (_countAdd == 0)
				{
					player.GetComponent<PlayerControl>()._money += 20;
					player.GetComponent<PhotonView>().RPC("UpdateMoney", PhotonTargets.Others, id);
					_countAdd++;
					StartCoroutine(WaitAddMoney());
					break;
				}
				else
				{
					break;
				}
			}
		}
	}
	/// <summary>
	/// Actualiza el texto del dinero
	/// </summary>
	/// <param name="id"> id del jugador al que tiene que actualizarle dinero</param>
	[PunRPC]
	public void UpdateMoney(int id)
	{
		if (photonView.isMine)
		{
			print("playerlocal " + GetComponent<PhotonView>().ownerId.ToString());
			if (id == GetComponent<PhotonView>().ownerId)
			{
				_moneyText.text = "Money: " + _money.ToString();
			}
		}
		else
		{
			print("no playerlocal " + GetComponent<PhotonView>().ownerId.ToString());
		}
	}

	//Funcion para actualizar vida
	[PunRPC]
	public void UpdateLife()
	{
		if (photonView.isMine)
		{
			print("playerlocal " + GetComponent<PhotonView>().ownerId.ToString());
			if (_life < 0)
			{
				_life = 0;
			}
			_lifeText.text = "Life: " + _life.ToString();

		}
		else
		{
			print("no playerlocal " + GetComponent<PhotonView>().ownerId.ToString());
		}
	}
	
	//Delay para actualizar el dinero
	IEnumerator WaitAddMoney()
	{
		yield return new WaitForSeconds(1);

		_countAdd = 0;
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
		}
	}

	[PunRPC]
	private void OnTriggerEnter(Collider other)
	{
		if (other.tag == "Bullet")
		{
			//Para localizar de quien es la bala, le asignamos un ID y lo pasamos por aquí, asignándonos como atacante
			_idDamage = other.GetComponent<BulletControl>().playerID;
			print(_idDamage + "idDamage");
			GetComponent<PhotonView>().RPC("GetDamage", PhotonTargets.All, 50, _idDamage);
			PhotonNetwork.Destroy(other.gameObject);
		}
	}

	private void ChangeState(ShootType NewType)
	{
		Type = NewType;
	}
}