using System;
using System.Collections.Generic;
using SimpleJSON;

namespace BackOfficeCommunication.Types {
    /// <summary>
    /// Parent class of all of the Back Office Property Type classes
    ///
    /// Handles all features shared by all of the property types:
    /// - labels
    /// </summary>
    public abstract class BOPropertyType {
        private Dictionary<string, string> _label;

        public BOPropertyType(JSONNode json) {
            _label = new Dictionary<string, string>();

            if (json["label"]["value"] == null || json["label"]["value"].AsObject == null) {
                return;
            }

            foreach (string key in json["label"]["value"].AsObject.getKeys()) {
                _label.Add(key, json["label"]["value"][key].Value);
            }
        }

        public string label {
            get {
                string label = Translation.getValue(_label);
                return !string.IsNullOrEmpty(label) ? label : "";
            }
        }

        public virtual JSONNode toJson() {
            throw new NotImplementedException();
        }

        public virtual string castToString() {
            return "-";
        }
    }
}