using SimpleJSON;

namespace BackOfficeCommunication.Types {
    
    
    /// <summary>
    /// Handles the Boolean Property Type from the Back Office
    ///
    /// Allows automatic casting to string and bool
    /// </summary>
    public class BOPropertyTypeBoolean : BOPropertyType {
        private bool _value = false;

        public BOPropertyTypeBoolean(JSONObject json) : base(json) {
            _value = json["value"]["default"].AsBool;
        }
        
        public bool value => _value;
        
        
        #region casting support
                
        public static implicit operator string(BOPropertyTypeBoolean m) {
            return UITranslator.getString($"common.{m.value.ToString().ToLower()}");
        }
        
        public static implicit operator bool(BOPropertyTypeBoolean m) {
            return m.value;
        }

        #endregion

        public override string castToString() {
            return UITranslator.getString($"common.{value.ToString()}".ToLower(), Translation.activeLanguage);
        }
    }
}