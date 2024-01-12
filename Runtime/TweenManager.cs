﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Zigurous.Tweening
{
    /// <summary>
    /// Manages the lifecycle of all tween objects, including updating,
    /// creating, destroying, and recycling tweens.
    /// </summary>
    [AddComponentMenu("")]
    internal sealed class TweenManager : MonoBehaviour
    {
        internal static volatile TweenManager instance;
        private static readonly object threadLock = new();
        private static bool isUnloading = false;

        private static TweenManager GetInstance()
        {
            if (instance == null)
            {
                lock (threadLock)
                {
                    instance = FindObjectOfType<TweenManager>();

                    if (instance == null && !isUnloading)
                    {
                        GameObject singleton = new()
                        {
                            name = typeof(TweenManager).Name,
                            hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector
                        };

                        return singleton.AddComponent<TweenManager>();
                    }
                }
            }

            return instance;
        }

        public static TweenManager Instance => GetInstance();
        public static bool HasInstance => instance != null;
        public static bool IsUnloading = isUnloading;

        internal List<Tween> tweens = new(Settings.initialCapacity);

        private void Awake()
        {
            isUnloading = false;

            if (instance == null)
            {
                instance = this;

                if (Application.isPlaying)
                {
                    SceneManager.sceneUnloaded += OnSceneUnloaded;
                    DontDestroyOnLoad(this);
                }
            }
            else
            {
                Destroy(this);
            }
        }

        private void OnDestroy()
        {
            isUnloading = true;

            if (instance == this)
            {
                instance = null;
                SceneManager.sceneUnloaded -= OnSceneUnloaded;
            }

            foreach (Tween tween in tweens) {
                tween.Kill();
            }

            tweens.Clear();
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            for (int i = tweens.Count - 1; i >= 0; i--)
            {
                Tween tween = tweens[i];

                switch (tween.internalState)
                {
                    case InternalTweenState.Active:
                        tween.Update(deltaTime);
                        break;

                    case InternalTweenState.Queued:
                        tween.internalState = InternalTweenState.Active;
                        break;

                    case InternalTweenState.Dequeued:
                        if (tween.recyclable) {
                            tween.internalState = InternalTweenState.Recycled;
                        } else {
                            tween.internalState = InternalTweenState.Killed;
                            tweens.RemoveAt(i);
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Recycles or creates a new tween object.
        /// </summary>
        internal Tweener<T,U> BuildTweener<T,U>()
        {
            Tweener<T,U> tweener = null;

            foreach (Tween tween in tweens)
            {
                if (tween.internalState == InternalTweenState.Recycled &&
                    tween.type == TweenType.Tweener &&
                    tween.template == typeof(Tweener<T,U>))
                {
                    tweener = (Tweener<T,U>)tween;
                    break;
                }
            }

            if (tweener == null) {
                tweener = new Tweener<T,U>();
            } else {
                tweener.Reset();
            }

            tweener.State = TweenState.Ready;
            tweener.internalState = InternalTweenState.Queued;

            return tweener;
        }

        /// <summary>
        /// Recycles or creates a new tween sequence.
        /// </summary>
        internal Sequence BuildSequence()
        {
            Sequence sequence = null;

            foreach (Tween tween in tweens)
            {
                if (tween.internalState == InternalTweenState.Recycled &&
                    tween.type == TweenType.Sequence)
                {
                    sequence = (Sequence)tween;
                    break;
                }
            }

            if (sequence == null) {
                sequence = new Sequence();
            } else {
                sequence.Reset();
            }

            sequence.State = TweenState.Ready;
            sequence.internalState = InternalTweenState.Queued;

            return sequence;
        }

        /// <summary>
        /// Adds a tween to the list of alive tweens so it can be managed and
        /// updated.
        /// </summary>
        internal void Track(Tween tween)
        {
            if (!tweens.Contains(tween)) {
                tweens.Add(tween);
            }
        }

        /// <summary>
        /// Kills all tweens that are animating objects on the unloaded scene.
        /// </summary>
        private void OnSceneUnloaded(Scene scene)
        {
            foreach (Tween tween in tweens)
            {
                if (tween.sceneIndex == -1 || tween.sceneIndex == scene.buildIndex) {
                    tween.Kill();
                }
            }
        }

    }

}
