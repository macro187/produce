using System.Collections.Generic;


namespace
produce
{


public interface
IPlugin
{


IEnumerable<Rule>
DetectRules(ProduceRepository repository);


}
}
