using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Singleton<T> where T : new() {
    public static T Instance {
        get {
            if (instance == null) {
                instance = new T();
            }
            return instance;
        }
    }

    private static T instance;
}
