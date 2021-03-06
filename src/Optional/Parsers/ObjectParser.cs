using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Optional.Exceptions;

namespace Optional.Parsers
{
    public class ObjectParser
    {
        public static Regex ShortOption = new Regex("^-[a-zA-Z0-9]{1}$");
        public static Regex ShortOptionWithValue = new Regex("^-[a-zA-Z0-9]{1}[:=]{1}(.+)$");
        public static Regex LongOption = new Regex("^--[-a-zA-Z0-9]{1,}$");
        public static Regex LongOptionWithValue = new Regex("^--[-a-zA-Z0-9]{1,}[:=]{1}([^:=]+)$");

        public Action<Option> OnDuplicateOption = option => { throw new DuplicateOptionException(option); };

        public Action<string> OnInvalidOption = name => { throw new InvalidOptionException(name); };

        public Action<string> OnMissingOption = arg => { throw new MissingOptionException(arg); };

        public T Parse<T>(string[] args) where T : new()
        {
            return Parse(args, new T());
        }

        public T Parse<T>(string[] args, T obj)
        {
            var availableOptions = Options.Create(obj);
            var setOptions = new List<Option>();
            Option current = null;

//            var options = new List<Option>();
//
//            var parser = new Parser
//                             {
//                                 OnShortOption = name => options.Add(new Option {ShortName = name}),
//                                 OnLongOption = name => options.Add(new Option {LongName = name}),
//                                 OnValue = value =>
//                                               {
//                                                   if (options.Count > 0)
//                                                   {
//                                                       var last = options[options.Count - 1];
//                                                       last.AddValue(value);
//                                                   }
//                                                   else
//                                                   {
//                                                       var option = new Option();
//                                                       option.AddValue(value);
//                                                       options.Add(option);
//                                                   }
//                                               }
//                             };
//            parser.Parse(args);

            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (ShortOption.IsMatch(arg))
                {
                    var name = arg.Substring(1);

                    var option = (from o in setOptions
                                  where o.ShortName == name
                                  select o).FirstOrDefault();
                    if (option != null)
                    {
                        OnDuplicateOption(option);
                    }

                    option = (from o in availableOptions
                              where o.ShortName == name
                              select o).FirstOrDefault();
                    if (option == null)
                    {
                        OnInvalidOption(name);
                    }

                    // TODO: duplicate code
                    if (option != null && option.Type == typeof(bool))
                    {
                        option.Property.SetValue(obj, true, null);
                    }

                    current = option;
                    setOptions.Add(current);
                    availableOptions.Remove(current);
                }
                else if (LongOption.IsMatch(arg))
                {
                    var name = arg.Substring(2);

                    var option = (from o in setOptions
                                  where o.LongName == name
                                  select o).FirstOrDefault();
                    if (option != null)
                    {
                        OnDuplicateOption(option);
                    }

                    option = (from o in availableOptions
                              where o.LongName == name
                              select o).FirstOrDefault();
                    if (option == null)
                    {
                        OnInvalidOption(name);
                    }

                    // TODO: duplicate code
                    if (option != null && option.Type == typeof(bool))
                    {
                        option.Property.SetValue(obj, true, null);
                    }

                    current = option;
                    setOptions.Add(current);
                    availableOptions.Remove(current);
                }
                else
                {
                    if (current == null)
                    {
                        OnMissingOption(arg);
                    }
                    else
                    {
                        current.AddValue(arg);
                        current.Property.SetValue(obj, arg, null);
                        current = null;
                    }
                }
            }

            foreach (var option in availableOptions)
            {
                var value = option.Property.GetValue(obj, null);
                if (option.Required && (value == null || String.IsNullOrEmpty(value.ToString())))
                {
                    throw new RequirementException(option);
                }
            }

            foreach (var option in setOptions)
            {
                if (option.Required && String.IsNullOrEmpty(option.Value))
                {
                    throw new RequirementException(option);
                }
            }

            return obj;
        }
    }
}