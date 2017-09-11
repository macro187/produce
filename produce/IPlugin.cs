using System.Collections.Generic;


namespace
produce
{


public interface
IPlugin
{


IEnumerable<Rule>
DetectWorkspaceRules(ProduceWorkspace workspace);


IEnumerable<Rule>
DetectRepositoryRules(ProduceRepository repository);


}
}
