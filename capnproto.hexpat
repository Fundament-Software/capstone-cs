#pragma endian little

import std.array;
import std.io;

using Word = u64;
using Pointer;

bitfield Bit { b: 1; };

namespace SpecialWords {
    bitfield StructPointer {
        A: 2 [[comment("Always 0")]];
        signed Offset: 30 [[comment("Offset in words from the end of the pointer to the start of the struct's data section. Signed")]];
        DataSectionSize: 16 [[comment("Size of the struct's data section in words.")]];
        PointerSectionSize: 16 [[comment("Size of the struct's data section in words.")]];
    } [[color("FF964F"), format("SpecialWords::format_struct_pointer")]];
    
    bitfield ListPointer {
        A: 2; // Always 1
        signed Offset: 30;
        ElementSize: 3;
        ListSize: 29;
    } [[color("A7C7E7"), format("SpecialWords::format_list_pointer")]];
    
    bitfield CompositeListTag {
        A: 2;
        ElementCount: 30;
        DataSize: 16;
        PointerSize: 16;
    };
    
    bitfield FarPointer {
        A: 2; //Always 2
        LandingPadSize: 1;
        Offset: 29;
        SegmentID: 32;
    } [[color("77DD77"), format("SpecialWords::format_far_pointer")]];
    
    bitfield CapabilityPointer {
        A: 2; //Always 3
        padding : 30;
        Index: 32;
    } [[color("ffdd3c")]];
    
    // -=-=-=( Formatting Functions )=-=-=-
    
    fn format_struct_pointer(StructPointer ptr) {
        return std::format("Struct: Offset {} words (Data: {} words, Pointers: {})", ptr.Offset, ptr.DataSectionSize, ptr.PointerSectionSize);
    };
    
    fn format_list_name(ListPointer l) {
        match (l.ElementSize) {
            (0): return "List(Void)";
            (1): return "List(Bit)";
            (2): return "List(Byte)";
            (3): return "List(Int16)";
            (4): return "List(Int32)";
            (5): return "List(Int64)";
            (6): return "List(Pointer)";
            (7): return "CompositeList";
        }
    };
    
    fn format_list_pointer(ListPointer l) {
        str name = SpecialWords::format_list_name(l);
        
        if (l.ElementSize < 7)
            return std::format("{}[{}]: offset {} words", name, l.ListSize, l.Offset);
        else
            return std::format("{}[{} words] : offset {} words", name, l.ListSize, l.Offset);
    };
    
    fn format_farpointer_name(FarPointer p) {
        if (p.LandingPadSize == 0) {
            return "FarPointer";
        }
        
        return "DoubleFarPointer";
    };
    
    fn format_far_pointer(FarPointer ptr) {
        str name = SpecialWords::format_farpointer_name(ptr);
        return std::format("{}: Segment {} + {} Words", name, ptr.SegmentID, ptr.Offset);
    };
}

// -=-=-=( SegmentTable )=-=-=-

struct SegmentTable {
    u32 count [[format("format_segment_count"), comment("Number of additional segments in the message. There is always at least one segment, so this is the total number of segments - 1")]];
    u32 segmentSizes[count + 1] [[comment("Size of each segment in words")]];
    padding[((count + 2) % 4) * 4];
};

fn format_segment_count(u32 segmentCount) {
    return std::format("{} (+ 1)", segmentCount);
};

SegmentTable segmentTable @ 0x00;

// -=-=-=( Offset Calculations )=-=-=-

// Calculates the absolute offset of the segment, by adding up segment sizes
// Takes into account that segments begin after the segment table
fn calc_segment_offset(u64 segmentID) {
    u128 address = sizeof(segmentTable);
    for(u64 i = 0, i < segmentID, i += 1) {
        address += segmentTable.segmentSizes[i] * sizeof(Word);
    }
    return address;
};

fn calc_offset(auto ptr) {
    return $ + (ptr.Offset * sizeof(Word));
};

fn calc_far_loc(SpecialWords::FarPointer ptr) {
    u128 address = calc_segment_offset(ptr.SegmentID);
    address += ptr.Offset * sizeof(Word);
    return address;
};

// -=-=-=( Struct )=-=-=-

struct StructContent<auto DataSize, auto PointerSize> {
    Word data[DataSize] [[color("583866")]];
    Pointer pointers[PointerSize];
};

struct StructPointer {
    SpecialWords::StructPointer ptrWord;
    bool isNull = (ptrWord.Offset | ptrWord.DataSectionSize | ptrWord.PointerSectionSize) == 0;
    bool isEmpty = (ptrWord.Offset == -1) && ((ptrWord.DataSectionSize | ptrWord.PointerSectionSize) == 0);
    
    if (!isNull && !isEmpty)
        StructContent<ptrWord.DataSectionSize, ptrWord.PointerSectionSize> content @ calc_offset(ptrWord) [[inline]];
} [[format("format_struct_pointer")]];

fn format_struct_pointer(StructPointer p) {
    if (p.isNull)
        return "Struct* : null";
    if (p.isEmpty)
        return "Struct* : Empty";

    auto ptrAddress = $ + addressof(p.content);
    return std::format(
            "Struct* : 0x{:X} (Data: {} words, Pointers: {})",
            ptrAddress,
            p.ptrWord.DataSectionSize,
            p.ptrWord.PointerSectionSize);
};

// -=-=-=( List ) =-=-=-

struct CompositeList {
    SpecialWords::CompositeListTag tag;
    StructContent<tag.DataSize, tag.PointerSize> list[tag.ElementCount];
};

struct ListContent<auto ElementSize, auto ListSize> {
    match(ElementSize) {
        (1): Bit list[ListSize];
        (2): u8 list[ListSize];
        (3): u16 list[ListSize];
        (4): u32 list[ListSize];
        (5): u64 list[ListSize];
        (6): Pointer list[ListSize];
        (7): CompositeList list;
    }
};

struct ListPointer {
    SpecialWords::ListPointer ptrWord;
    ListContent<ptrWord.ElementSize, ptrWord.ListSize> content @ calc_offset(ptrWord) [[inline]];
} [[format("format_list_pointer")]];

fn format_list_pointer(ListPointer l) {
    str name = SpecialWords::format_list_name(l.ptrWord);
    auto ptrAddress = $ + addressof(l.content);
    return std::format("{}* : 0x{:X} (Size: {})", name, ptrAddress, l.ptrWord.ListSize);
};

// -=-=-=-( Far Pointers )=-=-=-

struct DoubleFarLandingPad {
    SpecialWords::FarPointer landingPad;
    match($[$] & 3) {
        (0): {
            SpecialWords::StructPointer tag;
            StructContent<tag.DataSectionSize, tag.PointerSectionSize> content @ calc_far_loc(landingPad);
        }
        (1): {
            SpecialWords::ListPointer tag;
            ListContent<tag.ElementSize, tag.ListSize> content @ calc_far_loc(landingPad);
        }
        (_): Word tag;
    }
};

struct FarPointer {
    SpecialWords::FarPointer ptrWord;
    if (ptrWord.LandingPadSize == 0) {
        Pointer landingPad @ calc_far_loc(ptrWord);
    } else if (ptrWord.LandingPadSize == 1) {
        DoubleFarLandingPad landingPad @ calc_far_loc(ptrWord);
    }
} [[format("format_far_pointer")]];

fn format_far_pointer(FarPointer p) {
    str name = SpecialWords::format_farpointer_name(p.ptrWord);
    auto ptrAddress = $ + addressof(p.landingPad);
    return std::format(
            "{} : 0x{:X} (Segment {} + {} Words)",
            name,
            ptrAddress,
            p.ptrWord.SegmentID,
            p.ptrWord.Offset);
};

struct Pointer {
    u8 type = $[$] & 3;
    match(type) {
        (0): StructPointer ptr;
        (1): ListPointer ptr;
        (2): FarPointer ptr;
        (3): SpecialWords::CapabilityPointer ptr;
    }
} [[format("format_pointer")]];

fn format_pointer(Pointer ptr) {
    match(ptr.type) {
        (0): return "Struct Pointer";
        (1): return std::format("{} Pointer", SpecialWords::format_list_name(ptr.ptr.ptrWord));
        (2): return "Far Pointer";
        (3): return "Capability Pointer";
            
    }
};

Pointer root @ $ [[inline]];