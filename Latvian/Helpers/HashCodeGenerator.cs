﻿// Copyright 2014 Pēteris Ņikiforovs
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
    public static class HashCodeGenerator
    {
        public static int Create(params object[] args)
        {
            int hashCode = 27;
            for (int i = 0; i < args.Length; i++)
                hashCode = (13 * hashCode) + (args[i] != null ? args[i].GetHashCode() : 0);
            return hashCode;
        }
    }
}