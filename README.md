# AzureStorage

## Passing connection string via ```IReloadingManager```

```cs
var settingsManager = configuration.LoadSettings<TSettings>("SettingsUrl");
var connectionStringManager = settingsManager.ConnectionString(x => ...);
var tableStorage = AzureTableStorage<TEntity>.Create(connectionStringManager, "TableName", log)
```

## Using ```AzureTableEntity```

```AzureTabeStorage``` allows you to use types in property of entities, which Azure does not support originally. Moreover, ```AzureTableEntity``` allows you to perform ```InsertOrMerge``` operations correctly, even if your entity contains value-type properties, which is impossible with original ```TableEntity```. To use ```AzureTabeStorage```, you should inherit your entity class from the ```AzureTableEntity```. 

### Type conversion

All additional types are stored as the strings. Process of determining, how particular type is stored, is:

1. If the type is originally supported by Azure, it is used as it is.
1. If the type or given property of the entity has an assigned storage value serializer, which implements ```IStorageValueSerializer```, this serializer will be used to convert the value. To serialize the type as the Json, you can use existing serializer ```JsonStorageValueSerializer```, or you can create your own serializer, if you need different format. You can specify particular serializer for the given type or even entity property in the entity metamodel (see **Entity metamodel** section below).
1. If the type has ```System.ComponentModel.TypeConverter```, which is not inherited from the ```System.Object```, this type converter will be used to convert the value. For example, primitive framework types such as ```decimal``` or ```TimeSpan```, have already type converter, so you can use they out of the box.
1. Otherwise type can't be stored, and ```InvaliOperationException``` will be thrown. :exclamation: This is the breaking change, if you migrate from the ```TableEntity``` - ```TableEntity``` silently ignores properties with unknown types.

### Value-type merging

You can choose one of value-type merging strategy for particular entity type in the entity metamodel (see **Entity metamodel** section below). The strategies are described by the ```ValueTypeMergingStrategy``` enum:

1. ```Forbid``` - Forbids merging operations on the entities with value type properties. This is the default strategy. :exclamation: This is the breaking change, if you migrate from the ```TableEntity``` - ```TableEntity``` always updates value type properties, even if they weren't changed. This strategy is default to pay your attention when migrating from the ```TableEntity``` and you could consciously make your choice. 
1. ```UpdateAlways``` - Always update value type properties, even if they weren't changed. This strategy mimics original ```TableEntity``` behaviour. Use it if you don't care about correct merging of the value type properties and just want to preserve old behaviour of your legacy app. It's not recomended for new apps.
1. ```UpdateIfDirty``` - Update value type properties only if they were changed. This is the recommended strategy to choose. It requires some additional effort from you, but you get correct merging of the value type properties. To make it work, you should implement value type properties in your entities as properties with backing fields and calls protected ```AzureTableEntity``` method ```MarkValueTypePropertyAsDirty```, despite of value being changed or not.

**Examples**

```cs
public class Quote : AzureTableEntity
{
    public string AssetPair { get; set; }
    public decimal Ask
    {
        get => _ask;
        set
        {
            _ask = value;
            MarkValueTypePropertyAsDirty(nameof(Ask));
        }
    }
    public decimal Bid
    {
        get => _bid;
        set
        {
            _bid = value;
            MarkValueTypePropertyAsDirty(nameof(Bid));
        }
    }

    private decimal _ask;
    private decimal _bid;
}
```

### Entity metamodel

As you can notice, to specify different behaviours of your entities, you should fill up the entity metamodel. To do it, you should pass metamodel provider to the static method ```EntityMetamodel.Configure(IMetamodelProvider provider)```. You can use one of the (or even several at once) metamodel providers. Metamodel provider implements ```IMetamodelProvider``` and provides some way for you to specify metadata. Built-in metamodel providers:

1. ```AnnotationsBasedMetamodelProvider```. It is a preferred way of specifying metamodel. You can use annotation attributes on your types and properties to specify serializers and value type merging strategies. 
    * Use ```ValueSerializerAttribute``` on custom type, or particular entity property to specify custom serializer for it.
    * Use ```JsonValueSerializerAttribute``` on custom type, or particular entity property to specify Json serializer for it.
    * Use ```ValueTypeMergingStrategyAttribute``` on entity type to specify value type properties merging strategy for it.
1. ```ConventionBasedMetamodelProvider```. It is a good way of specifying default values for metamodel, or if you have tons of custom types or entities with well defined naming rules or namespace structure, for which you want to assign different serializers or value type properties meging strategies. Also it's applicable if you use types from a third party library in properties of your entities, and you want to specify serializers for these types.
    * If type or property matches multiple rules, which you registered, first matching rule (in order, which you registered them) will be used.
1. ```ImperativeMetamodelProvider```. It is a good way to speify serializers for types from a third party library, which you want to use in the properties of your entities and the number of these is not very big (a dozen is a reasonable limit).
1. ```CompositeMetamodelProvider```. It lets you combine all of the providers described above.
    * If type or property contained in the different providers, which you added to the ```CompositeMetamodelProvider```, metadata will be taken from the first (in order, which you added them) of the providers.
    
**Examples**

*AnnotationsBasedMetamodelProvider*

```cs
// === Models ===

[ValueSerializerAttribute(typeof(MyCsvStorageValueSerializer)]
public class QuoteHistoryItem
{
    public int Tick { get; set; }
    public decimal Ask { get; set; }
    public decimal Bid { get; set; }
}

// Preserves legacy value type properties merging behaviour
[ValueTypeMergingStrategyAttribute(ValueTypeMergingStrategy.UpdateAlways)]
public class QuoteHistory : AzureTableEntity
{
    public string AssetPair { get; set; }
    public virtual DateTime Timestamp { get; set; }
    // Will be stored in the Items field as CSV string (MyCsvStorageValueSerializer should be implemented)
    public virtual QuoteHistoryItem[] Items { get; set; }    
}

// Use correct value type properties merging mehaviour
[ValueTypeMergingStrategyAttribute(ValueTypeMergingStrategy.UpdateIfDirty)]
public class JsonFormatQuoteHistory : QuoteHistory
{
    // Will be stored int the Items field Json string
    [JsonValueSerializerAttribute]
    public overried QuoteHistoryItem[] Items { get; set; }    
}

// === Startup ===

var provider = new AnnotationsBasedMetamodelProvider();

EntityMetamodel.Configure(provider);
```

*ConventionBasedMetamodelProvider*

```cs
// === Models ===

public class QuoteHistory : AzureTableEntity
{
    public string AssetPair { get; set; }
    public virtual DateTime Timestamp { get; set; }
    public virtual ThirdParyQuoteHistoryItem[] Items { get; set; }    
}

public class JsonFormatQuoteHistory : QuoteHistory
{
    public overried ThirdParyQuoteHistoryItem[] Items { get; set; }    
}

// === Startup ===

// Ordered from specific rules to the common rules

var provider = new ConventionBasedMetamodelProvider()

    // Serialize all class-typed properties of the entities, 
    // which names is started form the Json using Json serializer

    .AddPropertySerializerRule(
        p => p.PropertyType.IsClass && p.ReflectingType.StartsWith("Json"),
        p => new JsonStorageValueSerializer())
        
    // Serialize all entity properties with type named ThirdParyQuoteHistoryItem 
    // using custom CSV serializer
        
    .AddTypeSerializerRule(
        t => t.Name == "ThirdParyQuoteHistoryItem",
        t => new MyCsvStorageValueSerializer())
        
    // Use UpdateIfDirty value-type properties meging strategy 
    // for the entity JsonFormatQuoteHistory
        
    .AddTypeValueTypesMergingStrategyRule(
        t => t == typeof(JsonFormatQuoteHistory),
        ValueTypeMergingStrategy.UpdateIfDirty)
    
    // Use UpdateAlways value-type properties merging strategy
    // for rest of entities
        
    .AddTypeValueTypesMergingStrategyRule(
        t => true,
        ValueTypeMergingStrategy.UpdateAlways);
        
EntityMetamodel.Configure(provider);
```

*ImperativeMetamodelProvider*

```cs
// === Models ===

public class QuoteHistory : AzureTableEntity
{
    public string AssetPair { get; set; }
    public virtual DateTime Timestamp { get; set; }
    public virtual ThirdParyQuoteHistoryItem[] Items { get; set; }    
}

public class JsonFormatQuoteHistory : QuoteHistory
{
    public overried ThirdParyQuoteHistoryItem[] Items { get; set; }    
}

// === Startup ===

var provider = new ImperativeMetamodelProvider()

    // Use Json serializer for the JsonFormatQuoteHistory.Items property
   
    .UseSerializer((JsonFormatQuoteHistory e) => e.Items, new JsonStorageValueSerializer())
        
    // Use custom CSV serializer for the entity properties with type ThirdParyQuoteHistoryItem
    // if it not overriden for particular property (e.g. JsonFormatQuoteHistory.Items)
        
    .UseSerializer<ThirdParyQuoteHistoryItem>(new MyCsvStorageValueSerializer())
        
    // Use UpdateIfDirty value-type properties meging strategy 
    // for the entity JsonFormatQuoteHistory
        
    .UseValueTypesMergingStrategy<JsonFormatQuoteHistory>(ValueTypeMergingStrategy.UpdateIfDirty)
    
    // Use UpdateAlways value-type properties merging strategy
    // for the entity QuoteHistory
        
    .UseValueTypesMergingStrategy<QuoteHistory>(ValueTypeMergingStrategy.UpdateAlways);
        
EntityMetamodel.Configure(provider);
```

*CompositeMetamodelProvider*

```cs
=== Startup ===

var provider = CompositeMetamodelProvider()
    .AddProvider(new AnnotationBasedMetamodelProvider())
    .AddProvider(ConfigureImperativeMetamodelProvider())
    .AddProvider(ConfigureConventionBasedMetamodelProvider());

EntityMetamodel.Configure(provider);  
```