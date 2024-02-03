﻿using System.Diagnostics;
using UnityEngine;

namespace Massive.Samples.Shooter
{
	public class ShooterSimulation : MonoBehaviour
	{
		[SerializeField] private int _simulationsPerFrame = 120;
		[SerializeField] private int _charactersCapacity = 10;
		[SerializeField] private int _bulletsCapacity = 1000;
		
		[Header("Entities")]
		[SerializeField] private EntityRoot<CharacterState> _characterPrefab;
		[SerializeField] private EntityRoot<BulletState> _bulletPrefab;

		private MassiveData<CharacterState> _characters;
		private MassiveData<BulletState> _bullets;
		private WorldUpdater[] _worldUpdaters;

		private EntitySynchronisation<CharacterState> _characterSynchronisation;
		private EntitySynchronisation<BulletState> _bulletSynchronisation;

		private void Awake()
		{
			_characters = new MassiveData<CharacterState>(framesCapacity: _simulationsPerFrame, dataCapacity: _charactersCapacity);
			_bullets = new MassiveData<BulletState>(framesCapacity: _simulationsPerFrame, dataCapacity: _bulletsCapacity);

			_worldUpdaters = FindObjectsOfType<WorldUpdater>();

			_characterSynchronisation = new EntitySynchronisation<CharacterState>(new EntityFactory<CharacterState>(_characterPrefab));
			_bulletSynchronisation = new EntitySynchronisation<BulletState>(new EntityFactory<BulletState>(_bulletPrefab));

			for (int i = 0; i < _charactersCapacity; i++)
			{
				_characters.Create(new CharacterState() { Transform = new EntityTransform() 
					{
						Position = Vector3.right * (i - _charactersCapacity / 2f) * 1.5f,
						Rotation = Quaternion.AngleAxis(180f * (i - _charactersCapacity / 2f) / _charactersCapacity, Vector3.forward)
					}});
			}
		}

		private int _currentFrame;

		private float _elapsedTime;
		
		private void Update()
		{
			Stopwatch stopwatch = Stopwatch.StartNew();

			if (_characters.CanRollbackFrames >= 0)
			{
				_currentFrame -= _characters.CanRollbackFrames;
				_characters.Rollback(_characters.CanRollbackFrames);
				_bullets.Rollback(_bullets.CanRollbackFrames);
			}

			_elapsedTime += Time.deltaTime;
			
			int targetFrame = Mathf.RoundToInt(_elapsedTime * 60);
			
			while (_currentFrame < targetFrame)
			{
				var world = new WorldFrame(_characters.CurrentFrame, _bullets.CurrentFrame, _currentFrame);
			
				foreach (var worldUpdater in _worldUpdaters)
				{
					worldUpdater.UpdateWorld(world);
				}

				_characters.SaveFrame();
				_bullets.SaveFrame();
				_currentFrame++;
			}
			
			var syncFrame = new WorldFrame(_characters.CurrentFrame, _bullets.CurrentFrame, _currentFrame);

			_characterSynchronisation.Synchronize(syncFrame.Characters);
			_bulletSynchronisation.Synchronize(syncFrame.Bullets);

			_debugTime = stopwatch.ElapsedMilliseconds;
		}

		private long _debugTime;
		
		private void OnGUI()
		{
			GUILayout.TextField($"{_debugTime}ms Simulation", new GUIStyle() { fontSize = 70, normal = new GUIStyleState() { textColor = Color.white } });
			GUILayout.TextField($"{_characters.CanRollbackFrames} Resimulations", new GUIStyle() { fontSize = 50, normal = new GUIStyleState() { textColor = Color.white } });
			GUILayout.TextField($"{_characters.CurrentFrame.AliveCount} Characters", new GUIStyle() { fontSize = 50, normal = new GUIStyleState() { textColor = Color.white } });
			GUILayout.TextField($"{_bullets.CurrentFrame.AliveCount} Bullets", new GUIStyle() { fontSize = 50, normal = new GUIStyleState() { textColor = Color.white } });
		}
	}
}