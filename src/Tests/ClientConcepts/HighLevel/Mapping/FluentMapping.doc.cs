using System;
using System.Collections.Generic;
using System.Reflection;
using Nest;
using Newtonsoft.Json;
using Tests.Framework;
using static Tests.Framework.RoundTripper;

namespace Tests.ClientConcepts.HighLevel.Mapping
{
	/**
	* [[fluent-mapping]]
	* === Fluent Mapping
	*
	* Fluent mapping POCO properties to fields within an Elasticsearch type mapping 
    * offers the most control over the process. With fluent mapping, each property of 
    * the POCO is explicitly mapped to an Elasticsearch type field mapping.
	*/

    public class FluentMapping
    {
        /**
		* To demonstrate, we'll define two POCOS 
        * 
        * - `Company`, which has a name and a collection of Employees
        * - `Employee` which has various properties of different types and has itself a collection of `Employee` types.
		*/

        public class Company
        {
            public string Name { get; set; }
            public List<Employee> Employees { get; set; }
        }

        public class Employee
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public int Salary { get; set; }
            public DateTime Birthday { get; set; }
            public bool IsManager { get; set; }
            public List<Employee> Employees { get; set; }
            public TimeSpan Hours { get; set; }
        }

        [U]
        public void MappingManually()
        {
            /**==== Manual mapping
			 * To create a mapping for our Company type, we can use the fluent API
			 * and map each property explicitly
			 */
            var descriptor = new CreateIndexDescriptor("myindex")
                .Mappings(ms => ms
                    .Map<Company>(m => m
                        .Properties(ps => ps
                            .Text(s => s
                                .Name(c => c.Name)
                            )
                            .Object<Employee>(o => o
                                .Name(c => c.Employees)
                                .Properties(eps => eps
                                    .Text(s => s
                                        .Name(e => e.FirstName)
                                    )
                                    .Text(s => s
                                        .Name(e => e.LastName)
                                    )
                                    .Number(n => n
                                        .Name(e => e.Salary)
                                        .Type(NumberType.Integer)
                                    )
                                )
                            )
                        )
                    )
                );

            /**
             * Here, the Name property of the `Company` type has been mapped as a {ref_current}/text.html[text datatype] and
             * the `Employees` property mapped as an {ref_current}/object.html[object datatype]. Within this object mapping,
             * only the `FirstName`, `LastName` and `Salary` properties of the `Employee` type have been mapped.
             * 
             * The json mapping for this example looks like
			 */
            // json
            var expected = new
            {
                mappings = new
                {
                    company = new
                    {
                        properties = new
                        {
                            name = new
                            {
                                type = "text"
                            },
                            employees = new
                            {
                                type = "object",
                                properties = new
                                {
                                    firstName = new
                                    {
                                        type = "text"
                                    },
                                    lastName = new
                                    {
                                        type = "text"
                                    },
                                    salary = new
                                    {
                                        type = "integer"
                                    }
                                }
                            }
                        }
                    }
                }
            };

            // hide
            Expect(expected).WhenSerializing((ICreateIndexRequest) descriptor);
        }

        /** Manual mapping in this way is great and useful but can become can become verbose and unwieldy for 
        * large POCOs. The majority of the time you simply want to map* all* the properties of a POCO in a single go
        * without having to specify the mapping for each, particularly when there is a known mapping
        * from CLR types to Elasticsearch types.
        *
        * This is where the fluent mapping in conjunction with automapping comes in.
        * 
        * ==== Auto mapping with fluent overrides
        * 
        * In most cases, you'll want to map more than just the vanilla datatypes and also provide
        * various options for your properties such as analyzer to use, whether to enable `doc_values`, etc.
        * 
        * In this case, it's possible to use `.AutoMap()` in conjunction with explicitly mapped properties.
        */
        [U]
        public void OverridingAutoMappedProperties()
        {
            /**
			* Here we are using `.AutoMap()` to automatically infer the mapping of our `Company` type from the
            * CLR property types but then we're overriding the `Employees` property to make it a 
            * {ref_current}/nested.html[nested datatype], since by default `.AutoMap()` will infer the 
            * `List<Employee>` property as an `object` datatype
			*/
            var descriptor = new CreateIndexDescriptor("myindex")
                .Mappings(ms => ms
                    .Map<Company>(m => m
                        .AutoMap()
                        .Properties(ps => ps
                            .Nested<Employee>(n => n
                                .Name(c => c.Employees)
                            )
                        )
                    )
                );

            /**
             */
            // json
            var expected = new
            {
                mappings = new
                {
                    company = new
                    {
                        properties = new
                        {
                            name = new
                            {
                                type = "text",
                                fields = new
                                {
                                    keyword = new
                                    {
                                        type = "keyword",
                                        ignore_above = 256
                                    }
                                }
                            },
                            employees = new
                            {
                                type = "nested",
                            }
                        }
                    }
                }
            };

            //hide
            Expect(expected).WhenSerializing((ICreateIndexRequest)descriptor);

            /**
			 * `.AutoMap()` **__is idempotent__** therefore calling it _before_ or _after_ 
             * manually mapped properties will still yield the same result. The next example
             * generates the same mapping as the previous
			 */
            descriptor = new CreateIndexDescriptor("myindex")
                .Mappings(ms => ms
                    .Map<Company>(m => m
                        .Properties(ps => ps
                            .Nested<Employee>(n => n
                                .Name(c => c.Employees)
                            )
                        )
                        .AutoMap()
                    )
                );

            //hide
            Expect(expected).WhenSerializing((ICreateIndexRequest)descriptor);
        }

        /**
        * ==== Auto mapping overrides down the object graph
        * 
        * You may have noticed in the previous example that the properties of the `Employees` property
        * were not mapped. This is because the automapping was applied only at the root of the `Company` mapping.
        * 
        * By calling `.AutoMap()` inside of the `.Nested<Employee>` mapping, you can also automap the 
        * `Employee` nested properties and again, override any inferred mapping from the automapping process,
        * through manual mapping
        */
        [U]
        public void OverridingDescendingAutoMappedProperties()
        {
            var descriptor = new CreateIndexDescriptor("myindex")
                .Mappings(m => m
                    .Map<Company>(mm => mm
                        .AutoMap() // <1> Automap `Company`
                        .Properties(p => p // <2> Override specific `Company` mappings
                            .Nested<Employee>(n => n
                                .Name(c => c.Employees)
                                .AutoMap() // <3> Automap `Employees` property
                                .Properties(pp => pp // <4> Override specific `Employee` properties
                                    .Text(t => t
                                        .Name(e => e.FirstName)
                                    )
                                    .Text(t => t
                                        .Name(e => e.LastName)
                                    )
                                    .Nested<Employee>(nn => nn
                                        .Name(e => e.Employees)
                                    )
                                )
                            )
                        )
                    )
                );

            /**
             */
            // json
            var expected = new
            {
                mappings = new
                {
                    company = new
                    {
                        properties = new
                        {
                            name = new
                            {
                                type = "text",
                                fields = new
                                {
                                    keyword = new
                                    {
                                        type = "keyword",
                                        ignore_above = 256
                                    }
                                }
                            },
                            employees = new
                            {
                                type = "nested",
                                properties = new
                                {
                                    firstName = new
                                    {
                                        type = "text"
                                    },
                                    lastName = new
                                    {
                                        type = "text"
                                    },
                                    salary = new
                                    {
                                        type = "integer"
                                    },
                                    birthday = new
                                    {
                                        type = "date"
                                    },
                                    isManager = new
                                    {
                                        type = "boolean"
                                    },
                                    employees = new
                                    {
                                        type = "nested"
                                    },
                                    hours = new
                                    {
                                        type = "long"
                                    }
                                }
                            }
                        }
                    }
                }
            };

            //hide
            Expect(expected).WhenSerializing((ICreateIndexRequest)descriptor);
        }
    }
}
