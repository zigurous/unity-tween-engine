﻿using UnityEngine;

namespace Zigurous.TweenEngine
{
    public static class Vector3Tweens
    {
        public static Tween TweenX(this Vector3 vector, float to, float duration) =>
            Tweening.To(getter: () => vector.x,
                        setter: x => vector.x = x,
                        to, duration).SetTarget(vector);

        public static Tween TweenY(this Vector3 vector, float to, float duration) =>
            Tweening.To(getter: () => vector.y,
                        setter: y => vector.y = y,
                        to, duration).SetTarget(vector);

        public static Tween TweenZ(this Vector3 vector, float to, float duration) =>
            Tweening.To(getter: () => vector.z,
                        setter: z => vector.z = z,
                        to, duration).SetTarget(vector);

    }

}