export interface ComicCheckedInterface {
  isbn: string;
  checkedItem: boolean;
}

export interface ComicListCheckedInterface {
  checkedItems: ComicCheckedInterface[];
}
