// Copyright 2014 Pēteris Ņikiforovs
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Latvian.Helpers
{
    static class HashCodeGenerator
    {
        public static int Create(object arg0)
        {
            int hashCode = 27;
            hashCode = (13 * hashCode) + (arg0 != null ? arg0.GetHashCode() : 0);
            return hashCode;
        }

        public static int Create(object arg0, object arg1)
        {
            int hashCode = 27;
            hashCode = (13 * hashCode) + (arg0 != null ? arg0.GetHashCode() : 0);
            hashCode = (13 * hashCode) + (arg1 != null ? arg1.GetHashCode() : 0);
            return hashCode;
        }

        public static int Create(object arg0, object arg1, object arg2)
        {
            int hashCode = 27;
            hashCode = (13 * hashCode) + (arg0 != null ? arg0.GetHashCode() : 0);
            hashCode = (13 * hashCode) + (arg1 != null ? arg1.GetHashCode() : 0);
            hashCode = (13 * hashCode) + (arg2 != null ? arg2.GetHashCode() : 0);
            return hashCode;
        }

        public static int Create(object arg0, object arg1, object arg2, object arg3)
        {
            int hashCode = 27;
            hashCode = (13 * hashCode) + (arg0 != null ? arg0.GetHashCode() : 0);
            hashCode = (13 * hashCode) + (arg1 != null ? arg1.GetHashCode() : 0);
            hashCode = (13 * hashCode) + (arg2 != null ? arg2.GetHashCode() : 0);
            hashCode = (13 * hashCode) + (arg3 != null ? arg3.GetHashCode() : 0);
            return hashCode;
        }

        public static int Create(params object[] args)
        {
            int hashCode = 27;
            for (int i = 0; i < args.Length; i++)
                hashCode = (13 * hashCode) + (args[i] != null ? args[i].GetHashCode() : 0);
            return hashCode;
        }
    }
}
